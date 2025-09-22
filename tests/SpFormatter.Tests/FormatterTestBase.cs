using FluentAssertions;
using Xunit;
using System.IO;
using System.Reflection;

namespace SpFormatter.Tests;

/// <summary>
/// Base class for all formatter tests providing common functionality
/// </summary>
public abstract class FormatterTestBase : IDisposable
{
    protected readonly SourcePawnFormatter _formatter;
    
    protected FormatterTestBase()
    {
        // Use Unix line endings for consistent test results across platforms
        var options = FormattingOptions.Default;
        options.LineEnding = "\n";
        _formatter = new SourcePawnFormatter(options);
    }
    
    /// <summary>
    /// Assert that input formats to expected output exactly
    /// </summary>
    protected void AssertFormatEquals(string input, string expected)
    {
        var result = _formatter.Format(input);
        result.Should().Be(expected, $"Input: '{input}' should format to: '{expected}'");
    }
    
    /// <summary>
    /// Assert that formatting preserves the input exactly (no changes needed)
    /// </summary>
    protected void AssertFormatPreserved(string input)
    {
        var result = _formatter.Format(input);
        result.Should().Be(input, $"Input: '{input}' should be preserved without changes");
    }
    
    /// <summary>
    /// Assert that formatting does not throw exceptions
    /// </summary>
    protected void AssertFormatDoesNotThrow(string input)
    {
        Action act = () => _formatter.Format(input);
        act.Should().NotThrow($"Input: '{input}' should format without throwing exceptions");
    }
    
    /// <summary>
    /// Assert that formatting produces compilable code (valid syntax)
    /// </summary>
    protected void AssertFormatProducesValidSyntax(string input)
    {
        var result = _formatter.Format(input);
        using var parser = new SourcePawnParser();
        
        var syntaxErrors = parser.GetSyntaxErrors(result);
        syntaxErrors.Should().BeEmpty($"Formatted output should have valid syntax. Output: '{result}'");
    }
    
    /// <summary>
    /// Assert that specific patterns exist in the formatted output
    /// </summary>
    protected void AssertFormatContains(string input, string pattern)
    {
        var result = _formatter.Format(input);
        result.Should().Contain(pattern, $"Formatted output should contain: '{pattern}'. Output: '{result}'");
    }
    
    /// <summary>
    /// Assert that specific patterns do not exist in the formatted output
    /// </summary>
    protected void AssertFormatDoesNotContain(string input, string pattern)
    {
        var result = _formatter.Format(input);
        result.Should().NotContain(pattern, $"Formatted output should not contain: '{pattern}'. Output: '{result}'");
    }
    
    /// <summary>
    /// Helper method to test formatting with specific options
    /// </summary>
    protected void AssertFormatEqualsWithOptions(string input, string expected, FormattingOptions options)
    {
        using var formatter = new SourcePawnFormatter(options);
        var formatted = formatter.Format(input);
        formatted.Should().Be(expected, $"Input: '{input}' with options should format to: '{expected}'");
    }

    /// <summary>
    /// Assert that formatting with options produces valid syntax
    /// </summary>
    protected void AssertFormatProducesValidSyntaxWithOptions(string input, FormattingOptions options)
    {
        using var formatter = new SourcePawnFormatter(options);
        var formatted = formatter.Format(input);

        using var parser = new SourcePawnParser();
        var syntaxErrors = parser.GetSyntaxErrors(formatted);
        syntaxErrors.Should().BeEmpty("Formatted output should have valid syntax.");
    }

    /// <summary>
    /// Assert that a test case file pair formats correctly with specific options
    /// </summary>
    protected void AssertFormatEqualsWithOptionsFromFiles(string inputFilePath, string expectedFilePath, FormattingOptions options)
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, inputFilePath);
        var expectedFile = Path.Combine(testCasesDir, expectedFilePath);

        File.Exists(inputFile).Should().BeTrue($"Input file should exist: {inputFile}");
        File.Exists(expectedFile).Should().BeTrue($"Expected file should exist: {expectedFile}");

        var input = File.ReadAllText(inputFile);
        var expected = File.ReadAllText(expectedFile);

        // Ensure options use Unix line endings for consistent test results
        options.LineEnding = "\n";
        using var formatter = new SourcePawnFormatter(options);
        var formatted = formatter.Format(input);
        formatted.Should().Be(expected, $"Input file '{inputFilePath}' with options should format to match '{expectedFilePath}'");
    }

    /// <summary>
    /// Assert that a test case file pair (input.sp + expected.sp) formats correctly
    /// </summary>
    protected void AssertTestCaseFormatsCorrectly(string testCaseName)
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, $"{testCaseName}_input.sp");
        var expectedFile = Path.Combine(testCasesDir, $"{testCaseName}_expected.sp");

        File.Exists(inputFile).Should().BeTrue($"Input file should exist: {inputFile}");
        File.Exists(expectedFile).Should().BeTrue($"Expected file should exist: {expectedFile}");

        var input = File.ReadAllText(inputFile);
        var expected = File.ReadAllText(expectedFile);

        var result = _formatter.Format(input);
        result.Should().Be(expected, $"Test case '{testCaseName}' should format correctly");
    }

    /// <summary>
    /// Assert that a test case file formats to valid syntax without checking exact output
    /// </summary>
    protected void AssertTestCaseProducesValidSyntax(string testCaseName)
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, $"{testCaseName}_input.sp");

        File.Exists(inputFile).Should().BeTrue($"Input file should exist: {inputFile}");

        var input = File.ReadAllText(inputFile);
        var result = _formatter.Format(input);

        using var parser = new SourcePawnParser();
        var syntaxErrors = parser.GetSyntaxErrors(result);
        syntaxErrors.Should().BeEmpty($"Test case '{testCaseName}' should produce valid syntax. Output: '{result}'");
    }

    /// <summary>
    /// Get all test case names in a specific category directory
    /// </summary>
    protected IEnumerable<string> GetTestCaseNamesInCategory(string category)
    {
        var categoryDir = Path.Combine(GetTestCasesDirectory(), category);
        if (!Directory.Exists(categoryDir))
            return Array.Empty<string>();

        var inputFiles = Directory.GetFiles(categoryDir, "*_input.sp", SearchOption.AllDirectories);
        return inputFiles.Select(f => Path.GetFileNameWithoutExtension(f).Replace("_input", ""));
    }

    protected string GetTestCasesDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(assemblyDir, "TestCases");
    }

    public void Dispose()
    {
        _formatter?.Dispose();
    }
}
