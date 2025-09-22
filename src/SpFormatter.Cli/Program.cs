using SpFormatter;

namespace SpFormatter.Cli;

public class Program
{
    public static void Main(string[] args)
    {
        bool writeToFile = false;
        bool verboseOutput = true;
        bool dryRun = false;
        bool processDirectory = false;
        bool createBackup = false;
        string inputFile = "";
        var inputFiles = new List<string>();
        
        // Parse command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--output":
                case "-o":
                    writeToFile = true;
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
                case "--help":
                case "-h":
                    ShowHelp();
                    return;
                default:
                    if (args[i].StartsWith("-"))
                    {
                        Console.WriteLine($"❌ Unknown option: {args[i]}");
                        Console.WriteLine("Use --help for usage information.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        inputFiles.Add(args[i]);
                        if (string.IsNullOrEmpty(inputFile))
                            inputFile = args[i];
                    }
                    break;
            }
        }

        if (verboseOutput)
        {
            Console.WriteLine("SourcePawn Formatter - CLI Tool");
            Console.WriteLine("===============================");
        }

        // Gather all files to process
        var filesToProcess = new List<string>();
        
        if (inputFiles.Count == 0)
        {
            // No files specified, use default test code
            if (verboseOutput)
            {
                Console.WriteLine("🧪 No files specified, using default test code (use --help for usage)");
            }
            ProcessDefaultCode(verboseOutput, writeToFile, dryRun);
            return;
        }
        
        // Process each input (file or directory)
        foreach (var input in inputFiles)
        {
            if (processDirectory || Directory.Exists(input))
            {
                // Directory processing
                if (Directory.Exists(input))
                {
                    var spFiles = Directory.GetFiles(input, "*.sp", SearchOption.AllDirectories);
                    var incFiles = Directory.GetFiles(input, "*.inc", SearchOption.AllDirectories);
                    filesToProcess.AddRange(spFiles);
                    filesToProcess.AddRange(incFiles);
                    
                    if (verboseOutput)
                    {
                        Console.WriteLine($"📁 Found {spFiles.Length} .sp files and {incFiles.Length} .inc files in {input}");
                    }
                }
                else
                {
                    Console.WriteLine($"❌ Directory not found: {input}");
                    Environment.Exit(1);
                }
            }
            else if (File.Exists(input))
            {
                // Single file
                filesToProcess.Add(input);
            }
            else
            {
                Console.WriteLine($"❌ File not found: {input}");
                Environment.Exit(1);
            }
        }
        
        if (verboseOutput)
        {
            Console.WriteLine($"📊 Processing {filesToProcess.Count} file(s)");
            if (dryRun)
                Console.WriteLine("🔍 DRY RUN MODE - No files will be modified");
            Console.WriteLine();
        }
        
        // Process all files
        ProcessFiles(filesToProcess, writeToFile, verboseOutput, dryRun, createBackup);
    }
    
    private static void ProcessDefaultCode(bool verboseOutput, bool writeToFile, bool dryRun)
    {
        var testCode = @"
public void OnPluginStart()
{
    HookEvent(""player_death"", Event_PlayerDeath);
    RegConsoleCmd(""sm_test"", Command_Test);
}

public Action Command_Test(int client, int args)
{
    if (IsValidClient(client))
    {
        PrintToChat(client, ""Hello World!"");
        return Plugin_Handled;
    }
    return Plugin_Continue;
}";

        ProcessSingleContent(testCode, "default", verboseOutput, writeToFile, dryRun, false);
    }
    
    private static void ProcessFiles(List<string> filesToProcess, bool writeToFile, bool verboseOutput, bool dryRun, bool createBackup)
    {
        int successCount = 0;
        int errorCount = 0;
        
        foreach (var file in filesToProcess)
        {
            try
            {
                if (verboseOutput)
                {
                    Console.WriteLine($"📝 Processing: {file}");
                }
                
                var content = File.ReadAllText(file);
                var result = ProcessSingleContent(content, file, verboseOutput && filesToProcess.Count == 1, writeToFile, dryRun, createBackup);
                
                if (result)
                {
                    successCount++;
                    if (verboseOutput && filesToProcess.Count > 1)
                    {
                        Console.WriteLine($"✅ {file}");
                    }
                }
                else
                {
                    errorCount++;
                    if (verboseOutput && filesToProcess.Count > 1)
                    {
                        Console.WriteLine($"❌ {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                Console.WriteLine($"❌ Error processing {file}: {ex.Message}");
            }
        }
        
        if (verboseOutput && filesToProcess.Count > 1)
        {
            Console.WriteLine();
            Console.WriteLine($"📊 Summary: {successCount} successful, {errorCount} errors");
        }
        
        if (errorCount > 0)
        {
            Environment.Exit(1);
        }
    }
    
    private static bool ProcessSingleContent(string content, string filename, bool showDetails, bool writeToFile, bool dryRun, bool createBackup)
    {
        try
        {
            using var parser = new SourcePawnParser();
            
            if (showDetails)
            {
                Console.WriteLine("✅ SourcePawn parser initialized successfully!");
                Console.WriteLine();
            }
            
            // Check for syntax errors first
            var syntaxErrors = parser.GetSyntaxErrors(content);
            if (syntaxErrors.Count > 0)
            {
                Console.WriteLine($"⚠️ Syntax errors found in {filename}:");
                foreach (var error in syntaxErrors.Take(5)) // Show first 5 errors
                {
                    Console.WriteLine($"  Line {error.StartLine}:{error.StartColumn} - {error.Message}");
                }
                if (syntaxErrors.Count > 5)
                {
                    Console.WriteLine($"  ... and {syntaxErrors.Count - 5} more errors");
                }
                Console.WriteLine("Continuing with formatting (may produce incomplete results)");
                Console.WriteLine();
            }
            
            if (showDetails && syntaxErrors.Count == 0)
            {
                Console.WriteLine("✅ Code parsed successfully - valid syntax!");
                Console.WriteLine();
            }
            
            // Format the code
            using var formatter = new SourcePawnFormatter();
            var formatted = formatter.Format(content);
            
            if (filename != "default" && (writeToFile || dryRun))
            {
                var outputPath = writeToFile ? 
                    Path.GetFileNameWithoutExtension(filename) + "_formatted" + Path.GetExtension(filename) :
                    filename;
                    
                if (dryRun)
                {
                    // Dry run: just report what would be done
                    var changes = content != formatted;
                    if (changes)
                    {
                        Console.WriteLine($"🔍 Would modify: {filename} → {outputPath}");
                    }
                    else if (showDetails)
                    {
                        Console.WriteLine($"🔍 No changes needed: {filename}");
                    }
                }
                else
                {
                    // Create backup if requested
                    if (createBackup && !writeToFile)
                    {
                        var backupPath = filename + ".bak";
                        File.Copy(filename, backupPath, true);
                        if (showDetails)
                        {
                            Console.WriteLine($"💾 Backup created: {backupPath}");
                        }
                    }
                    
                    // Write formatted content
                    File.WriteAllText(outputPath, formatted);
                    if (showDetails || !writeToFile)
                    {
                        Console.WriteLine($"✅ Formatted code written to: {outputPath}");
                    }
                }
            }
            else
            {
                // Output to console
                if (showDetails)
                {
                    Console.WriteLine("=== Formatted Code ===");
                }
                Console.WriteLine(formatted);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing {filename}: {ex.Message}");
            return false;
        }
    }
    
    private static void ShowHelp()
    {
        Console.WriteLine("SourcePawn Formatter - CLI Tool");
        Console.WriteLine("===============================");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  SpFormatter.Cli [files/directories...] [options]");
        Console.WriteLine();
        Console.WriteLine("Arguments:");
        Console.WriteLine("  files                SourcePawn files (.sp, .inc) to format");
        Console.WriteLine("  directories          Directories to process (use --dir to enable)");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -o, --output         Write formatted code to [filename]_formatted.sp");
        Console.WriteLine("  -q, --quiet          Suppress verbose output");
        Console.WriteLine("  -d, --dry-run        Show what would be changed without modifying files");
        Console.WriteLine("  -b, --backup         Create .bak files before in-place formatting");
        Console.WriteLine("      --dir            Enable directory processing (recursive)");
        Console.WriteLine("  -h, --help           Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  SpFormatter.Cli test.sp                    # Format single file to console");
        Console.WriteLine("  SpFormatter.Cli test.sp --output           # Write to test_formatted.sp");
        Console.WriteLine("  SpFormatter.Cli test.sp --backup           # Format in-place with backup");
        Console.WriteLine("  SpFormatter.Cli *.sp --output              # Format multiple files");
        Console.WriteLine("  SpFormatter.Cli src/ --dir                 # Format all .sp files in directory");
        Console.WriteLine("  SpFormatter.Cli src/ --dir --dry-run       # Preview directory changes");
        Console.WriteLine("  SpFormatter.Cli test.sp --quiet --output   # Silent processing");
        Console.WriteLine();
        Console.WriteLine("Features:");
        Console.WriteLine("  • Single file and batch processing");
        Console.WriteLine("  • Recursive directory processing");
        Console.WriteLine("  • Syntax error detection and reporting");
        Console.WriteLine("  • Dry-run mode for safe previewing");
        Console.WriteLine("  • Automatic backup creation");
        Console.WriteLine("  • Support for .sp and .inc files");
    }
}