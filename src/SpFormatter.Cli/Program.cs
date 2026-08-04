using SpFormatter;

namespace SpFormatter.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        bool writeSidecar = false;
        bool verboseOutput = true;
        bool dryRun = false;
        bool processDirectory = false;
        bool createBackup = false;
        bool checkOnly = false;
        bool readStdin = false;
        var inputFiles = new List<string>();
        var options = FormattingOptions.Default;

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
                case "--indent":
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out var indent) || indent < 0)
                    {
                        Console.Error.WriteLine("error: --indent requires a non-negative integer");
                        return 1;
                    }
                    options.IndentSize = indent;
                    options.UseSpaces = true;
                    break;
                case "--use-tabs":
                    options.UseSpaces = false;
                    break;
                case "--space-before-paren":
                    options.SpaceBeforeOpenParen = true;
                    break;
                case "--no-space-around-operators":
                    options.SpaceAroundOperators = false;
                    break;
                case "--unsafe-macros":
                    options.AllowUnsafeMacros = true;
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

        if (readStdin)
        {
            if (inputFiles.Count > 0)
            {
                Console.Error.WriteLine("error: --stdin does not take file arguments");
                return 1;
            }

            if (writeSidecar || dryRun || createBackup || checkOnly || processDirectory)
            {
                Console.Error.WriteLine("error: --stdin cannot be combined with --output, --dry-run, --backup, --check, or --dir");
                return 1;
            }

            return ProcessStdin(options);
        }

        if (verboseOutput)
        {
            Console.WriteLine("SourcePawn Formatter - CLI Tool");
            Console.WriteLine("===============================");
        }

        if (inputFiles.Count == 0)
        {
            if (verboseOutput)
                Console.WriteLine("No files specified, using default test code (use --help for usage)");
            return ProcessDefaultCode(verboseOutput, options) ? 0 : 1;
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

                var spFiles = Directory.GetFiles(input, "*.sp", SearchOption.AllDirectories);
                var incFiles = Directory.GetFiles(input, "*.inc", SearchOption.AllDirectories);
                filesToProcess.AddRange(spFiles);
                filesToProcess.AddRange(incFiles);

                if (verboseOutput)
                    Console.WriteLine($"Found {spFiles.Length} .sp files and {incFiles.Length} .inc files in {input}");
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

        if (verboseOutput)
        {
            Console.WriteLine($"Processing {filesToProcess.Count} file(s)");
            if (dryRun)
                Console.WriteLine("DRY RUN - no files will be modified");
            if (checkOnly)
                Console.WriteLine("CHECK - exit non-zero if formatting would change a file");
            Console.WriteLine();
        }

        return ProcessFiles(filesToProcess, writeSidecar, verboseOutput, dryRun, createBackup, checkOnly, options);
    }

    private static int ProcessStdin(FormattingOptions options)
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

        try
        {
            using var formatter = new SourcePawnFormatter(options);
            var formatResult = formatter.FormatWithResult(content);
            if (!formatResult.Success)
            {
                Console.Error.WriteLine(
                    $"error formatting stdin: {formatResult.Errors.FirstOrDefault()?.Message}");
                return 1;
            }

            Console.Out.Write(formatResult.Text);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error formatting stdin: {ex.Message}");
            return 1;
        }
    }

    private static bool ProcessDefaultCode(bool verboseOutput, FormattingOptions options)
    {
        var testCode = """
public void OnPluginStart()
{
    HookEvent("player_death", Event_PlayerDeath);
    RegConsoleCmd("sm_test", Command_Test);
}

public Action Command_Test(int client, int args)
{
    if (IsValidClient(client))
    {
        PrintToChat(client, "Hello World!");
        return Plugin_Handled;
    }
    return Plugin_Continue;
}
""";

        return ProcessFileContent(
            testCode,
            "default",
            showDetails: verboseOutput,
            writeSidecar: false,
            dryRun: false,
            createBackup: false,
            checkOnly: false,
            options).Success;
    }

    private static int ProcessFiles(
        List<string> filesToProcess,
        bool writeSidecar,
        bool verboseOutput,
        bool dryRun,
        bool createBackup,
        bool checkOnly,
        FormattingOptions options)
    {
        int successCount = 0;
        int errorCount = 0;
        int driftCount = 0;

        foreach (var file in filesToProcess)
        {
            try
            {
                if (verboseOutput)
                    Console.WriteLine($"Processing: {file}");

                var content = File.ReadAllText(file);
                var result = ProcessFileContent(
                    content,
                    file,
                    showDetails: verboseOutput && filesToProcess.Count == 1,
                    writeSidecar,
                    dryRun,
                    createBackup,
                    checkOnly,
                    options);

                if (!result.Success)
                {
                    errorCount++;
                    if (verboseOutput && filesToProcess.Count > 1)
                        Console.WriteLine($"failed: {file}");
                    continue;
                }

                successCount++;
                if (result.WouldChange)
                    driftCount++;

                if (verboseOutput && filesToProcess.Count > 1)
                    Console.WriteLine($"ok: {file}");
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.Error.WriteLine($"error processing {file}: {ex.Message}");
            }
        }

        if (verboseOutput && filesToProcess.Count > 1)
        {
            Console.WriteLine();
            Console.WriteLine($"Summary: {successCount} successful, {errorCount} errors, {driftCount} would change");
        }

        if (errorCount > 0)
            return 1;
        if (checkOnly && driftCount > 0)
            return 2;
        return 0;
    }

    private sealed record ProcessOutcome(bool Success, bool WouldChange);

    private static ProcessOutcome ProcessFileContent(
        string content,
        string filename,
        bool showDetails,
        bool writeSidecar,
        bool dryRun,
        bool createBackup,
        bool checkOnly,
        FormattingOptions options)
    {
        try
        {
            using var parser = new SourcePawnParser();

            if (showDetails)
            {
                Console.WriteLine("SourcePawn parser initialized successfully!");
                Console.WriteLine();
            }

            var syntaxErrors = parser.GetSyntaxErrors(content);
            if (syntaxErrors.Count > 0)
            {
                Console.WriteLine($"Syntax errors found in {filename}:");
                foreach (var error in syntaxErrors.Take(5))
                    Console.WriteLine($"  Line {error.StartLine}:{error.StartColumn} - {error.Message}");
                if (syntaxErrors.Count > 5)
                    Console.WriteLine($"  ... and {syntaxErrors.Count - 5} more errors");
                Console.WriteLine("Continuing with formatting (may produce incomplete results)");
                Console.WriteLine();
            }
            else if (showDetails)
            {
                Console.WriteLine("Code parsed successfully - valid syntax!");
                Console.WriteLine();
            }

            using var formatter = new SourcePawnFormatter(options);
            var formatResult = formatter.FormatWithResult(content);
            if (!formatResult.Success)
            {
                Console.Error.WriteLine($"error formatting {filename}: {formatResult.Errors.FirstOrDefault()?.Message}");
                return new ProcessOutcome(false, false);
            }

            var formatted = formatResult.Text;
            var wouldChange = !string.Equals(
                content.Replace("\r\n", "\n"),
                formatted.Replace("\r\n", "\n"),
                StringComparison.Ordinal);

            if (filename == "default")
            {
                if (showDetails)
                    Console.WriteLine("=== Formatted Code ===");
                Console.WriteLine(formatted);
                return new ProcessOutcome(true, wouldChange);
            }

            if (checkOnly)
            {
                if (wouldChange)
                    Console.WriteLine($"would reformat: {filename}");
                else if (showDetails)
                    Console.WriteLine($"already formatted: {filename}");
                return new ProcessOutcome(true, wouldChange);
            }

            if (dryRun)
            {
                if (wouldChange)
                {
                    var target = writeSidecar
                        ? Path.GetFileNameWithoutExtension(filename) + "_formatted" + Path.GetExtension(filename)
                        : filename;
                    Console.WriteLine($"Would modify: {filename} -> {target}");
                }
                else if (showDetails)
                {
                    Console.WriteLine($"No changes needed: {filename}");
                }

                return new ProcessOutcome(true, wouldChange);
            }

            if (createBackup)
            {
                var backupPath = filename + ".bak";
                File.Copy(filename, backupPath, true);
                if (showDetails)
                    Console.WriteLine($"Backup created: {backupPath}");
                File.WriteAllText(filename, formatted);
                if (showDetails)
                    Console.WriteLine($"Formatted in place: {filename}");
                return new ProcessOutcome(true, wouldChange);
            }

            if (writeSidecar)
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(filename))!;
                var outputName = Path.GetFileNameWithoutExtension(filename) + "_formatted" + Path.GetExtension(filename);
                var fullOutput = Path.Combine(directory, outputName);
                File.WriteAllText(fullOutput, formatted);
                Console.WriteLine($"Formatted code written to: {fullOutput}");
                return new ProcessOutcome(true, wouldChange);
            }

            if (showDetails)
                Console.WriteLine("=== Formatted Code ===");
            Console.WriteLine(formatted);
            return new ProcessOutcome(true, wouldChange);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error processing {filename}: {ex.Message}");
            return new ProcessOutcome(false, false);
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("""
SourcePawn Formatter - CLI Tool

Usage:
  SpFormatter.Cli [files/directories...] [options]

Arguments:
  files                SourcePawn files (.sp, .inc) to format
  directories          Directories to process (use --dir to enable)

Options:
  -o, --output         Write formatted code to [filename]_formatted.sp
      --stdin          Read source from stdin; write formatted source to stdout
  -q, --quiet          Suppress verbose output
  -d, --dry-run        Show what would be changed without modifying files
  -b, --backup         Create .bak files and write formatted code in place
      --check          Exit non-zero if any file would change
      --dir            Enable directory processing (recursive)
      --indent <n>     Indent size when using spaces
      --use-tabs       Indent with tabs
      --space-before-paren
                       Space before '(' on calls and function names
                       (control headers always use a space)
      --no-space-around-operators
                       Disable spaces around operators
      --unsafe-macros  Format files that contain function-like #define macros
  -h, --help           Show this help message

Examples:
  SpFormatter.Cli test.sp
  SpFormatter.Cli test.sp --output
  SpFormatter.Cli test.sp --backup
  SpFormatter.Cli src/ --dir --check
  SpFormatter.Cli plugin.sp --indent 2 --quiet
  SpFormatter.Cli --stdin --indent 4 < plugin.sp
""");
    }
}
