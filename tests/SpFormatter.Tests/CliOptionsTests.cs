using FluentAssertions;
using System.IO;
using System.Diagnostics;
using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// Tests for CLI command-line options and functionality.
/// These tests verify that the CLI tool works correctly with various arguments.
/// </summary>
public class CliOptionsTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _cliPath;

    public CliOptionsTests()
    {
        // Create a temporary directory for test files
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"sp_formatter_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        // Determine the CLI executable path (assuming it's built)
        var currentDir = Directory.GetCurrentDirectory();
        var projectRoot = FindProjectRoot(currentDir);
        _cliPath = Path.Combine(projectRoot, "src", "SpFormatter.Cli", "bin", "Debug", "net9.0", "SpFormatter.Cli.exe");
    }

    private static string FindProjectRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SpFormatter.sln")))
        {
            directory = directory.Parent;
        }
        return directory?.FullName ?? startPath;
    }

    #region Helper Methods

    private void CreateTestFile(string filename, string content)
    {
        var filePath = Path.Combine(_tempDirectory, filename);
        File.WriteAllText(filePath, content);
    }

    private string RunCli(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _cliPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = _tempDirectory
        };

        using var process = Process.Start(startInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start CLI process");

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException($"CLI failed: {error}");

        return output;
    }

    private bool CliExists()
    {
        return File.Exists(_cliPath);
    }

    #endregion

    #region Basic Functionality Tests

    [Fact]
    public void TestCli_Help_Option()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var output = RunCli("--help");
        
        output.Should().Contain("SourcePawn Formatter");
        output.Should().Contain("Usage:");
        output.Should().Contain("--output");
        output.Should().Contain("--quiet");
        output.Should().Contain("--dry-run");
        output.Should().Contain("--directory");
        output.Should().Contain("--backup");
    }

    [Fact]
    public void TestCli_ShortHelp_Option()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var output = RunCli("-h");
        
        output.Should().Contain("SourcePawn Formatter");
        output.Should().Contain("Usage:");
    }

    [Fact]
    public void TestCli_NoFiles_UsesDefaultCode()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var output = RunCli("--quiet");
        
        output.Should().NotBeEmpty();
        // Should contain formatted default test code
        output.Should().Contain("native");
    }

    #endregion

    #region Single File Processing

    [Fact]
    public void TestCli_SingleFile_Processing()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = @"native bool IsClientValid(int client);
int g_iCount=0;
public void OnPluginStart(){PrintToServer(""Plugin started"");}";
        
        CreateTestFile("test.sp", testContent);
        
        var output = RunCli($"test.sp --quiet");
        
        output.Should().Contain("native bool IsClientValid(int client);");
        output.Should().Contain("int g_iCount = 0;"); // Should add spaces around =
        output.Should().Contain("public void OnPluginStart()"); // Should add space after )
    }

    [Fact]
    public void TestCli_OutputFlag_CreatesFile()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "int x=5;";
        CreateTestFile("input.sp", testContent);
        
        RunCli("input.sp --output --quiet");
        
        var outputFile = Path.Combine(_tempDirectory, "input_formatted.sp");
        File.Exists(outputFile).Should().BeTrue("Output file should be created");
        
        var formattedContent = File.ReadAllText(outputFile);
        formattedContent.Should().Contain("int x = 5;"); // Should add spaces around =
    }

    [Fact]
    public void TestCli_OutputFlag_ShortForm()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "float y=2.5;";
        CreateTestFile("input2.sp", testContent);
        
        RunCli("input2.sp -o --quiet");
        
        var outputFile = Path.Combine(_tempDirectory, "input2_formatted.sp");
        File.Exists(outputFile).Should().BeTrue("Output file should be created with -o flag");
        
        var formattedContent = File.ReadAllText(outputFile);
        formattedContent.Should().Contain("float y = 2.5;");
    }

    #endregion

    #region Quiet Mode Tests

    [Fact]
    public void TestCli_QuietFlag_SuppressesVerboseOutput()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "int x = 5;";
        CreateTestFile("quiet_test.sp", testContent);
        
        var verboseOutput = RunCli("quiet_test.sp");
        var quietOutput = RunCli("quiet_test.sp --quiet");
        
        verboseOutput.Should().Contain("SourcePawn Formatter - CLI Tool");
        quietOutput.Should().NotContain("SourcePawn Formatter - CLI Tool");
        
        // Both should contain the actual formatted code
        verboseOutput.Should().Contain("int x = 5;");
        quietOutput.Should().Contain("int x = 5;");
    }

    [Fact]
    public void TestCli_QuietFlag_ShortForm()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "bool flag = true;";
        CreateTestFile("quiet_test2.sp", testContent);
        
        var output = RunCli("quiet_test2.sp -q");
        
        output.Should().NotContain("SourcePawn Formatter - CLI Tool");
        output.Should().Contain("bool flag = true;");
    }

    #endregion

    #region Dry Run Tests

    [Fact]
    public void TestCli_DryRunFlag_DoesNotModifyFiles()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "int x=5;";
        var originalFile = Path.Combine(_tempDirectory, "dryrun_test.sp");
        CreateTestFile("dryrun_test.sp", testContent);
        
        var originalModifyTime = File.GetLastWriteTime(originalFile);
        
        // Wait a moment to ensure modify time would change if file was written
        Thread.Sleep(100);
        
        var output = RunCli("dryrun_test.sp --dry-run --quiet");
        
        // File should not be modified
        File.GetLastWriteTime(originalFile).Should().Be(originalModifyTime);
        
        // Should indicate what would be done
        output.Should().Contain("Would modify:");
    }

    [Fact]
    public void TestCli_DryRunFlag_ShortForm()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "float y=2.5;";
        CreateTestFile("dryrun_test2.sp", testContent);
        
        var output = RunCli("dryrun_test2.sp -d --quiet");
        
        output.Should().Contain("Would modify:");
    }

    #endregion

    #region Directory Processing Tests

    [Fact]
    public void TestCli_DirectoryFlag_ProcessesAllFiles()
    {
        if (!CliExists()) return; // Skip if CLI not built

        // Create a subdirectory with multiple .sp files
        var subDir = Path.Combine(_tempDirectory, "subdir");
        Directory.CreateDirectory(subDir);
        
        CreateTestFile("file1.sp", "int x=1;");
        CreateTestFile("file2.sp", "float y=2.0;");
        File.WriteAllText(Path.Combine(subDir, "file3.sp"), "bool z=true;");
        
        var output = RunCli($"{_tempDirectory} --directory --dry-run --quiet");
        
        output.Should().Contain("Found");
        output.Should().Contain(".sp files");
        output.Should().Contain("Would modify:");
    }

    [Fact]
    public void TestCli_DirectoryFlag_ShortForm()
    {
        if (!CliExists()) return; // Skip if CLI not built

        CreateTestFile("dirtest.sp", "int value=42;");
        
        var output = RunCli($"{_tempDirectory} --dir --dry-run --quiet");
        
        output.Should().Contain("Found");
        output.Should().Contain(".sp files");
    }

    #endregion

    #region Backup Tests

    [Fact]
    public void TestCli_BackupFlag_CreatesBackupFile()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "int original=123;";
        var originalFile = Path.Combine(_tempDirectory, "backup_test.sp");
        CreateTestFile("backup_test.sp", testContent);
        
        RunCli("backup_test.sp --backup --quiet");
        
        var backupFile = originalFile + ".bak";
        File.Exists(backupFile).Should().BeTrue("Backup file should be created");
        
        var backupContent = File.ReadAllText(backupFile);
        backupContent.Should().Be(testContent, "Backup should contain original content");
        
        var modifiedContent = File.ReadAllText(originalFile);
        modifiedContent.Should().Contain("int original = 123;", "Original file should be formatted");
    }

    [Fact]
    public void TestCli_BackupFlag_ShortForm()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "float pi=3.14;";
        CreateTestFile("backup_test2.sp", testContent);
        
        RunCli("backup_test2.sp -b --quiet");
        
        var backupFile = Path.Combine(_tempDirectory, "backup_test2.sp.bak");
        File.Exists(backupFile).Should().BeTrue("Backup file should be created with -b flag");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void TestCli_NonExistentFile_ShowsError()
    {
        if (!CliExists()) return; // Skip if CLI not built

        try
        {
            var output = RunCli("nonexistent.sp --quiet");
            output.Should().Contain("not found");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Contain("not found");
        }
    }

    [Fact]
    public void TestCli_InvalidOption_ShowsError()
    {
        if (!CliExists()) return; // Skip if CLI not built

        try
        {
            var output = RunCli("--invalid-option");
            output.Should().Contain("Unknown option");
        }
        catch (InvalidOperationException ex)
        {
            ex.Message.Should().Contain("Unknown option");
        }
    }

    #endregion

    #region Combined Options Tests

    [Fact]
    public void TestCli_CombinedOptions_OutputAndQuiet()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "native void TestFunction(int param);";
        CreateTestFile("combined_test.sp", testContent);
        
        var output = RunCli("combined_test.sp --output --quiet");
        
        // Should be minimal output in quiet mode
        output.Should().NotContain("SourcePawn Formatter - CLI Tool");
        
        // Output file should be created
        var outputFile = Path.Combine(_tempDirectory, "combined_test_formatted.sp");
        File.Exists(outputFile).Should().BeTrue();
    }

    [Fact]
    public void TestCli_CombinedOptions_DryRunAndBackup()
    {
        if (!CliExists()) return; // Skip if CLI not built

        var testContent = "int test=999;";
        var originalFile = Path.Combine(_tempDirectory, "drybackup_test.sp");
        CreateTestFile("drybackup_test.sp", testContent);
        
        RunCli("drybackup_test.sp --dry-run --backup --quiet");
        
        // In dry-run mode, no backup should be created
        var backupFile = originalFile + ".bak";
        File.Exists(backupFile).Should().BeFalse("No backup should be created in dry-run mode");
        
        // Original file should be unchanged
        var content = File.ReadAllText(originalFile);
        content.Should().Be(testContent, "Original file should be unchanged in dry-run mode");
    }

    #endregion

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
