#:project ../src/SpFormatter/SpFormatter.csproj
#:property JsonSerializerIsReflectionEnabledByDefault=true

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using SpFormatter;

// File-based app (no .csproj). From repo root:
//   dotnet run tools/corpus-probe.cs -- <corpusRoot> [--limit N] [--out report.json]

if (args.Length < 1)
{
    Console.Error.WriteLine("Usage: dotnet run tools/corpus-probe.cs -- <corpusRoot> [--limit N] [--out report.json]");
    return 1;
}

var corpusRoot = Path.GetFullPath(args[0]);
var limit = 0;
var outPath = Path.Combine("artifacts", "corpus-probe-report.json");
for (var i = 1; i < args.Length; i++)
{
    if (args[i] == "--limit" && i + 1 < args.Length && int.TryParse(args[++i], out var n))
        limit = n;
    else if (args[i] == "--out" && i + 1 < args.Length)
        outPath = Path.GetFullPath(args[++i]);
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

Console.WriteLine($"Probing {files.Count} .sp files under {corpusRoot}");
var sw = Stopwatch.StartNew();

var options = new FormattingOptions { LineEnding = "\n" };
var tallies = new ConcurrentDictionary<string, int>();
var samples = new ConcurrentDictionary<string, ConcurrentBag<string>>();

void Bump(string key, string? samplePath = null)
{
    tallies.AddOrUpdate(key, 1, static (_, n) => n + 1);
    if (samplePath == null)
        return;
    var bag = samples.GetOrAdd(key, _ => new ConcurrentBag<string>());
    if (bag.Count < 8)
        bag.Add(samplePath);
}

Parallel.ForEach(
    files,
    new ParallelOptions { MaxDegreeOfParallelism = Math.Max(2, Environment.ProcessorCount - 1) },
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

        try
        {
            using var formatter = new SourcePawnFormatter(options);
            var once = formatter.FormatWithResult(source);
            if (!once.Success)
            {
                var msg = once.Errors.FirstOrDefault()?.Message ?? "";
                if (msg.Contains("function-like #define", StringComparison.Ordinal))
                    Bump("refuse_macros", rel);
                else
                    Bump("format_fail", rel);
                return;
            }

            var normalizedIn = Normalize(source);
            var normalizedOut = Normalize(once.Text);
            if (normalizedIn == normalizedOut)
            {
                Bump("success_unchanged", rel);
                return;
            }

            var twice = formatter.FormatWithResult(once.Text);
            if (!twice.Success)
            {
                Bump("second_pass_fail", rel);
                return;
            }

            if (Normalize(twice.Text) == normalizedOut)
                Bump("success_changed_idempotent", rel);
            else
                Bump("success_changed_not_idempotent", rel);
        }
        catch (Exception ex)
        {
            Bump("exception", rel + " :: " + ex.GetType().Name);
        }
    });

sw.Stop();

var report = new StringBuilder();
report.AppendLine("{");
report.AppendLine($"  \"corpusRoot\": {JsonSerializer.Serialize(corpusRoot)},");
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

report.AppendLine("  }");
report.AppendLine("}");

var json = report.ToString();
var fullOut = Path.GetFullPath(outPath);
Directory.CreateDirectory(Path.GetDirectoryName(fullOut)!);
File.WriteAllText(fullOut, json);
Console.WriteLine(json);
Console.WriteLine($"Wrote {fullOut}");
return 0;

static string Normalize(string s) =>
    s.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
