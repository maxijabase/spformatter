#:project ../src/SpFormatter/SpFormatter.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SpFormatter;

// Compile-preserve confidence layer over the format probe.
// Uses spcomp --syntax-only so no .smx files are written.
//
// From repo root:
//   dotnet run tools/corpus-compile-probe.cs -- <corpusRoot>
//     --spcomp <path\to\spcomp.exe>
//     [--include <path>] [--limit N] [--out report.json]
//
// Only files that format successfully (and re-parse) are compile-checked.
// Baseline = original source. Preserve = formatted source must match baseline
// success/failure for the interesting case "baseline ok => formatted ok".

if (args.Length < 1)
{
    Console.Error.WriteLine(
        "Usage: dotnet run tools/corpus-compile-probe.cs -- <corpusRoot> --spcomp <spcomp.exe> [--include <dir>] [--limit N] [--out report.json]");
    return 1;
}

var corpusRoot = Path.GetFullPath(args[0]);
var spcompPath = "";
var includePath = "";
var limit = 0;
var outPath = Path.Combine("artifacts", "corpus-compile-probe-report.json");

for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--spcomp" && i + 1 < args.Length)
        spcompPath = Path.GetFullPath(args[++i]);
    else if (args[i] == "--include" && i + 1 < args.Length)
        includePath = Path.GetFullPath(args[++i]);
    else if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var n))
        limit = n;
    else if (args[i] == "--out" && i + 1 < args.Length)
        outPath = Path.GetFullPath(args[++i]);
}

if (string.IsNullOrWhiteSpace(spcompPath) || !File.Exists(spcompPath))
{
    Console.Error.WriteLine("Missing or invalid --spcomp path.");
    return 1;
}

if (string.IsNullOrWhiteSpace(includePath))
{
    var sibling = Path.Combine(Path.GetDirectoryName(spcompPath)!, "include");
    if (Directory.Exists(sibling))
        includePath = sibling;
}

if (string.IsNullOrWhiteSpace(includePath) || !Directory.Exists(includePath))
{
    Console.Error.WriteLine("Missing include dir. Pass --include <sourcemod scripting/include>.");
    return 1;
}

var skip = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
var skipList = Path.Combine(corpusRoot, "corpus_skip.list");
if (File.Exists(skipList))
{
    foreach (var line in File.ReadLines(skipList))
    {
        var t = line.Trim().Replace('/', Path.DirectorySeparatorChar);
        if (t.Length > 0 && !t.StartsWith('#'))
            skip.Add(t);
    }
}

var files = Directory.EnumerateFiles(corpusRoot, "*.sp", SearchOption.AllDirectories)
    .Select(f => Path.GetRelativePath(corpusRoot, f))
    .Where(rel => !rel.StartsWith("spformatter", StringComparison.OrdinalIgnoreCase))
    .Where(rel => !skip.Contains(rel))
    .OrderBy(rel => rel, StringComparer.OrdinalIgnoreCase)
    .ToList();

if (limit > 0 && files.Count > limit)
    files = files.Take(limit).ToList();

var workRoot = Path.Combine(Path.GetTempPath(), "spformatter-compile-probe-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(workRoot);

Console.WriteLine($"Compile-preserve probe: {files.Count} files");
Console.WriteLine($"spcomp={spcompPath}");
Console.WriteLine($"include={includePath}");
Console.WriteLine($"workRoot={workRoot} (temp .sp only; --syntax-only writes no .smx)");

var sw = Stopwatch.StartNew();
var tallies = new ConcurrentDictionary<string, int>();
var samples = new ConcurrentDictionary<string, ConcurrentBag<string>>();

// Keep full path lists for buckets we triage (sample bags alone hide the rest).
var fullLists = new ConcurrentDictionary<string, ConcurrentBag<string>>(StringComparer.Ordinal);

void Bump(string key, string? samplePath = null, bool keepAll = false)
{
    tallies.AddOrUpdate(key, 1, static (_, n) => n + 1);
    if (samplePath == null)
        return;
    var bag = samples.GetOrAdd(key, _ => new ConcurrentBag<string>());
    if (bag.Count < 32)
        bag.Add(samplePath);
    if (keepAll)
        fullLists.GetOrAdd(key, _ => new ConcurrentBag<string>()).Add(samplePath);
}

static string DetectLineEnding(string source)
{
    var crlf = 0;
    var lf = 0;
    for (var i = 0; i < source.Length; i++)
    {
        if (source[i] != '\n')
            continue;
        if (i > 0 && source[i - 1] == '\r')
            crlf++;
        else
            lf++;
    }

    if (crlf == 0 && lf == 0)
        return Environment.NewLine;
    return crlf >= lf ? "\r\n" : "\n";
}

try
{
    Parallel.ForEach(
        files,
        new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) },
        rel =>
        {
            var path = Path.Combine(corpusRoot, rel);
            string source;
            try
            {
                source = File.ReadAllText(path);
            }
            catch
            {
                Bump("read_error", rel);
                return;
            }

            string formatted;
            try
            {
                // Match input newline style. Forcing LF breaks some spcomp `\`-continued
                // #define / string macros that only compile with CRLF.
                var options = new FormattingOptions { LineEnding = DetectLineEnding(source) };
                using var formatter = new SourcePawnFormatter(options);
                var once = formatter.FormatWithResult(source);
                if (!once.Success)
                {
                    var msg = once.Errors.FirstOrDefault()?.Message ?? "";
                    if (msg.Contains("function-like #define", StringComparison.Ordinal))
                        Bump("skip_refuse_macros", rel, keepAll: true);
                    else
                        Bump("skip_format_fail", rel, keepAll: true);
                    return;
                }

                var twice = formatter.FormatWithResult(once.Text);
                if (!twice.Success)
                {
                    Bump("skip_second_pass_fail", rel, keepAll: true);
                    return;
                }

                formatted = once.Text;
            }
            catch (Exception ex)
            {
                Bump("skip_exception", rel + " :: " + ex.GetType().Name, keepAll: true);
                return;
            }

            var activeDir = Path.GetDirectoryName(path)!;
            var baseline = RunSyntaxOnly(spcompPath, includePath, activeDir, path, sourceFileIsTemp: false);

            var tempSp = Path.Combine(workRoot, Guid.NewGuid().ToString("N") + ".sp");
            try
            {
                File.WriteAllText(tempSp, formatted, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                var after = RunSyntaxOnly(spcompPath, includePath, activeDir, tempSp, sourceFileIsTemp: true);

                if (baseline.Ok && after.Ok)
                {
                    Bump("compile_preserve_ok", rel);
                    return;
                }

                if (baseline.Ok && !after.Ok)
                {
                    Bump("compile_broke", rel, keepAll: true);
                    Bump("compile_broke_detail", rel + " :: " + Head(after.Output), keepAll: true);
                    return;
                }

                if (!baseline.Ok && after.Ok)
                {
                    Bump("compile_fixed", rel, keepAll: true);
                    return;
                }

                Bump("compile_baseline_and_formatted_fail", rel);
            }
            finally
            {
                try
                {
                    if (File.Exists(tempSp))
                        File.Delete(tempSp);
                }
                catch
                {
                    // best-effort cleanup
                }
            }
        });
}
finally
{
    try
    {
        if (Directory.Exists(workRoot))
            Directory.Delete(workRoot, recursive: true);
    }
    catch
    {
        Console.Error.WriteLine("Warning: could not delete temp workRoot: " + workRoot);
    }
}

sw.Stop();

var report = new StringBuilder();
report.AppendLine("{");
report.AppendLine($"  \"corpusRoot\": {JsonSerializer.Serialize(corpusRoot)},");
report.AppendLine($"  \"spcomp\": {JsonSerializer.Serialize(spcompPath)},");
report.AppendLine($"  \"include\": {JsonSerializer.Serialize(includePath)},");
report.AppendLine($"  \"files\": {files.Count},");
report.AppendLine($"  \"elapsedMs\": {sw.ElapsedMilliseconds},");
report.AppendLine("  \"tallies\": {");
var keys = tallies.Keys.OrderBy(k => k).ToList();
for (var i = 0; i < keys.Count; i++)
{
    var k = keys[i];
    var comma = i + 1 < keys.Count ? "," : "";
    report.AppendLine($"    {JsonSerializer.Serialize(k)}: {tallies[k]}{comma}");
}

report.AppendLine("  },");
report.AppendLine("  \"samples\": {");
var skeys = samples.Keys.OrderBy(k => k).ToList();
for (var i = 0; i < skeys.Count; i++)
{
    var k = skeys[i];
    var arr = samples[k].ToArray();
    var comma = i + 1 < skeys.Count ? "," : "";
    report.AppendLine($"    {JsonSerializer.Serialize(k)}: {JsonSerializer.Serialize(arr)}{comma}");
}

report.AppendLine("  },");
report.AppendLine("  \"lists\": {");
var lkeys = fullLists.Keys.OrderBy(k => k).ToList();
for (var i = 0; i < lkeys.Count; i++)
{
    var k = lkeys[i];
    var arr = fullLists[k].ToArray().OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToArray();
    var comma = i + 1 < lkeys.Count ? "," : "";
    report.AppendLine($"    {JsonSerializer.Serialize(k)}: {JsonSerializer.Serialize(arr)}{comma}");
}

report.AppendLine("  }");
report.AppendLine("}");

var json = report.ToString();
var fullOut = Path.GetFullPath(outPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullOut)!);
File.WriteAllText(fullOut, json);
Console.WriteLine(json);
Console.WriteLine($"Wrote {fullOut}");
return 0;

static string Head(string text)
{
    var one = text.Replace("\r\n", "\n").Replace('\r', '\n');
    var lines = one.Split('\n');
    // Prefer the first real diagnostic over the trailing "N Errors." summary line.
    foreach (var raw in lines)
    {
        var line = raw.Trim();
        if (line.Length == 0)
            continue;
        if (line.Contains("error ", StringComparison.OrdinalIgnoreCase)
            || line.Contains(" : error ", StringComparison.Ordinal))
            return line.Length > 160 ? line[..160] : line;
    }

    var fallback = lines.FirstOrDefault(l => l.Trim().Length > 0)?.Trim() ?? "";
    return fallback.Length > 160 ? fallback[..160] : fallback;
}

static (bool Ok, string Output) RunSyntaxOnly(
    string spcomp,
    string includeDir,
    string activeDir,
    string sourcePath,
    bool sourceFileIsTemp)
{
    // --syntax-only: dry-run, no .smx output (spcomp 1.11+ / 1.12).
    // -D sets the active directory so sibling #include "x.inc" still resolve
    // for formatted temp files copied out of tree.
    var args = new StringBuilder();
    args.Append("--syntax-only ");
    args.Append("-v0 ");
    args.Append("--use-stderr ");
    args.Append("-i").Append(QuoteArg(includeDir)).Append(' ');
    args.Append("-D").Append(QuoteArg(activeDir)).Append(' ');
    args.Append(QuoteArg(sourcePath));

    using var proc = new Process();
    proc.StartInfo = new ProcessStartInfo
    {
        FileName = spcomp,
        Arguments = args.ToString(),
        WorkingDirectory = activeDir,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };

    var stdout = new StringBuilder();
    var stderr = new StringBuilder();
    proc.OutputDataReceived += (_, e) =>
    {
        if (e.Data != null)
            stdout.AppendLine(e.Data);
    };
    proc.ErrorDataReceived += (_, e) =>
    {
        if (e.Data != null)
            stderr.AppendLine(e.Data);
    };

    try
    {
        proc.Start();
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();
        if (!proc.WaitForExit(60_000))
        {
            try
            {
                proc.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignore
            }

            return (false, "timeout");
        }

        // Drain async readers.
        proc.WaitForExit();
        var output = stdout.ToString() + stderr.ToString();
        _ = sourceFileIsTemp;
        return (proc.ExitCode == 0, output);
    }
    catch (Exception ex)
    {
        return (false, ex.GetType().Name + ": " + ex.Message);
    }
}

static string QuoteArg(string value)
{
    if (value.Contains(' ') || value.Contains('"'))
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    return value;
}
