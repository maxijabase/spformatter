using SpFormatter;
using SpModernizer;

namespace SpModernizer.Cli;

public static class Program
{
    public static int Main(string[] args)
    {
        bool writeSidecar = false;
        bool verboseOutput = true;
        bool dryRun = false;
        bool showDiff = false;
        bool processDirectory = false;
        bool createBackup = false;
        bool checkOnly = false;
        bool readStdin = false;
        bool formatAfter = true;
        var inputFiles = new List<string>();
        var enabledRules = new List<string>();
        var excludedRules = new List<string>();
        var formattingOptions = FormattingOptions.Default;
        bool allowUnsafeMacros = false;

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--output":
                case "-o":
                    writeSidecar = true;
                    break;
                case "--stdin":
                    readStdin = true;
                    verboseOutput = false;
                    break;
                case "--quiet":
                case "-q":
                    verboseOutput = false;
                    break;
                case "--dry-run":
                case "-d":
                    dryRun = true;
                    break;
                case "--diff":
                    showDiff = true;
                    dryRun = true;
                    break;
                case "--directory":
                case "--dir":
                    processDirectory = true;
                    break;
                case "--backup":
                case "-b":
                    createBackup = true;
                    break;
                case "--check":
                    checkOnly = true;
                    break;
                case "--no-format":
                    formatAfter = false;
                    break;
                case "--unsafe-macros":
                    allowUnsafeMacros = true;
                    break;
                case "--rules":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("error: --rules requires a comma-separated list");
                        return 1;
                    }

                    enabledRules.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--exclude":
                    if (i + 1 >= args.Length)
                    {
                        Console.Error.WriteLine("error: --exclude requires a comma-separated list");
                        return 1;
                    }

                    excludedRules.AddRange(args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--indent":
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out var indent) || indent < 0)
                    {
                        Console.Error.WriteLine("error: --indent requires a non-negative integer");
                        return 1;
                    }

                    formattingOptions.IndentSize = indent;
                    formattingOptions.UseSpaces = true;
                    break;
                case "--use-tabs":
                    formattingOptions.UseSpaces = false;
                    break;
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;
                default:
                    if (args[i].StartsWith('-'))
                    {
                        Console.Error.WriteLine($"error: unknown option {args[i]}");
                        Console.Error.WriteLine("use --help for usage information.");
                        return 1;
                    }

                    inputFiles.Add(args[i]);
                    break;
            }
        }

        var modernizeOptions = new ModernizeOptions
        {
            FormatAfter = formatAfter,
            FormattingOptions = formattingOptions,
            AllowUnsafeMacros = allowUnsafeMacros,
            EnabledRules = enabledRules,
            ExcludedRules = excludedRules,
        };

        if (readStdin)
        {
            if (inputFiles.Count > 0)
            {
                Console.Error.WriteLine("error: --stdin does not take file arguments");
                return 1;
            }

            return ProcessStdin(modernizeOptions);
        }

        if (verboseOutput)
        {
            Console.WriteLine("SourcePawn Modernizer - CLI Tool");
            Console.WriteLine("================================");
        }

        if (inputFiles.Count == 0)
        {
            if (verboseOutput)
                Console.WriteLine("No files specified, using default demo (use --help for usage)");
            return ProcessDemo(verboseOutput, modernizeOptions) ? 0 : 1;
        }

        var filesToProcess = new List<string>();
        foreach (var input in inputFiles)
        {
            if (processDirectory || Directory.Exists(input))
            {
                if (!Directory.Exists(input))
                {
                    Console.Error.WriteLine($"error: directory not found: {input}");
                    return 1;
                }

                filesToProcess.AddRange(Directory.GetFiles(input, "*.sp", SearchOption.AllDirectories));
                filesToProcess.AddRange(Directory.GetFiles(input, "*.inc", SearchOption.AllDirectories));
            }
            else if (File.Exists(input))
            {
                filesToProcess.Add(input);
            }
            else
            {
                Console.Error.WriteLine($"error: file not found: {input}");
                return 1;
            }
        }

        return ProcessFiles(filesToProcess, writeSidecar, verboseOutput, dryRun, showDiff, createBackup, checkOnly, modernizeOptions);
    }

    private static int ProcessStdin(ModernizeOptions options)
    {
        string content;
        try
        {
            content = Console.In.ReadToEnd();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error reading stdin: {ex.Message}");
            return 1;
        }

        using var modernizer = new SourcePawnModernizer(options);
        var result = modernizer.ModernizeWithResult(content);
        if (!result.Success)
        {
            Console.Error.WriteLine($"error: {result.Errors.FirstOrDefault()?.Message}");
            return 1;
        }

        Console.Out.Write(result.Text);
        return 0;
    }

    private static bool ProcessDemo(bool verbose, ModernizeOptions options)
    {
        const string demo = """
new Float:x = 5.0;
new y = 7;

public OnPluginStart()
{
    new Float:z = Float:0;
}
""";
        using var modernizer = new SourcePawnModernizer(options);
        var result = modernizer.ModernizeWithResult(demo);
        if (!result.Success)
        {
            Console.Error.WriteLine(result.Errors.FirstOrDefault()?.Message);
            return false;
        }

        if (verbose)
            Console.WriteLine("=== Modernized ===");
        Console.WriteLine(result.Text);
        return true;
    }

    private static int ProcessFiles(
        List<string> files,
        bool writeSidecar,
        bool verbose,
        bool dryRun,
        bool showDiff,
        bool createBackup,
        bool checkOnly,
        ModernizeOptions options)
    {
        int errors = 0;
        int drift = 0;

        foreach (var file in files)
        {
            try
            {
                var content = File.ReadAllText(file);
                using var modernizer = new SourcePawnModernizer(options);
                var result = modernizer.ModernizeWithResult(content);
                if (!result.Success)
                {
                    errors++;
                    Console.Error.WriteLine($"error modernizing {file}: {result.Errors.FirstOrDefault()?.Message}");
                    continue;
                }

                var normalizedIn = content.Replace("\r\n", "\n").Replace("\r", "\n");
                var normalizedOut = result.Text.Replace("\r\n", "\n").Replace("\r", "\n");
                var wouldChange = !string.Equals(normalizedIn, normalizedOut, StringComparison.Ordinal);

                if (verbose)
                {
                    Console.WriteLine(file);
                    if (result.Changes.Count > 0)
                        Console.WriteLine($"  changes: {result.Changes.Count}");
                    foreach (var d in result.Diagnostics)
                        Console.WriteLine($"  diagnostic [{d.RuleId}]: {d.Message}");
                }

                if (wouldChange)
                    drift++;

                if (showDiff && wouldChange)
                    WriteUnifiedDiff(file, normalizedIn, normalizedOut);

                if (checkOnly || dryRun)
                    continue;

                if (writeSidecar)
                {
                    var ext = Path.GetExtension(file);
                    var sidecar = Path.ChangeExtension(file, null) + "_modernized" + ext;
                    File.WriteAllText(sidecar, result.Text);
                }
                else if (createBackup)
                {
                    File.Copy(file, file + ".bak", overwrite: true);
                    File.WriteAllText(file, result.Text);
                }
                else if (files.Count == 1 && !writeSidecar && !createBackup)
                {
                    Console.Write(result.Text);
                }
                else
                {
                    File.WriteAllText(file, result.Text);
                }
            }
            catch (Exception ex)
            {
                errors++;
                Console.Error.WriteLine($"error processing {file}: {ex.Message}");
            }
        }

        if (errors > 0)
            return 1;
        if (checkOnly && drift > 0)
            return 2;
        return 0;
    }

    private static void WriteUnifiedDiff(string path, string before, string after)
    {
        var beforeLines = before.Replace("\r\n", "\n").Split('\n');
        var afterLines = after.Replace("\r\n", "\n").Split('\n');
        Console.WriteLine($"--- {path}");
        Console.WriteLine($"+++ {path} (modernized)");
        var max = Math.Max(beforeLines.Length, afterLines.Length);
        for (var i = 0; i < max; i++)
        {
            var b = i < beforeLines.Length ? beforeLines[i] : null;
            var a = i < afterLines.Length ? afterLines[i] : null;
            if (b == a)
                continue;
            if (b != null)
                Console.WriteLine("-" + b);
            if (a != null)
                Console.WriteLine("+" + a);
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("""
SourcePawn Modernizer CLI

Usage:
  SpModernizer.Cli [options] <files-or-dirs>

Options:
  --stdin              Read source from stdin, write to stdout
  -o, --output         Write *_modernized.ext sidecars
  -b, --backup         Backup to .bak and overwrite in place
  -d, --dry-run        Report changes without writing
  --diff               Show a line diff (implies dry-run)
  --check              Exit 2 if files would change
  --dir, --directory   Treat arguments as directories
  --rules a,b          Enable only these rule ids
  --exclude a,b        Disable these rule ids
  --no-format          Skip FormatAfter (CLI formats by default)
  --unsafe-macros      Allow function-like #define
  --indent n           Formatting indent size
  --use-tabs           Formatting tabs
  -q, --quiet          Less banner output
  -h, --help           Show help

Rule ids:
  old-type-cast, old-builtins, old-types, old-variables,
  tagged-signatures, multi-tag, functag, funcenum,
  old-struct-fields, legacy-while
""");
    }
}
