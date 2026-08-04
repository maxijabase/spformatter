using FluentAssertions;
using System.IO;
using System.Reflection;
using Xunit;

namespace SpFormatter.Tests;

public class SourcePawnParserTests : IDisposable
{
    private readonly SourcePawnParser _parser;

    public SourcePawnParserTests()
    {
        _parser = new SourcePawnParser();
    }

    private string GetTestCasesDirectory()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(assemblyDir, "TestCases");
    }

    private string ReadTestCase(string testCaseFilePath)
    {
        var testCasesDir = GetTestCasesDirectory();
        var fullPath = Path.Combine(testCasesDir, testCaseFilePath);
        return File.ReadAllText(fullPath);
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        using var parser = new SourcePawnParser();
        parser.Should().NotBeNull();
    }

    [Fact]
    public void ParseSource_WithEmptyString_ShouldReturnNull()
    {
        var result = _parser.ParseSource("");
        result.Should().BeNull();
    }

    [Fact]
    public void ParseSource_WithNullString_ShouldReturnNull()
    {
        var result = _parser.ParseSource(null!);
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("SourcePawnParser/ValidSyntax/simple_function_input.sp")]
    [InlineData("SourcePawnParser/ValidSyntax/native_declaration_input.sp")]
    [InlineData("SourcePawnParser/ValidSyntax/variable_declaration_input.sp")]
    public void ParseSource_WithValidCode_ShouldReturnTree(string testCaseFile)
    {
        var sourceCode = ReadTestCase(testCaseFile);
        using var result = _parser.ParseSource(sourceCode);

        result.Should().NotBeNull();
        result!.RootNode.Should().NotBeNull();
    }

    [Theory]
    [InlineData("SourcePawnParser/ValidSyntax/simple_function_input.sp")]
    [InlineData("SourcePawnParser/ValidSyntax/native_declaration_input.sp")]
    [InlineData("SourcePawnParser/ValidSyntax/variable_declaration_input.sp")]
    public void IsValidSyntax_WithValidCode_ShouldReturnTrue(string testCaseFile)
    {
        var sourceCode = ReadTestCase(testCaseFile);
        var result = _parser.IsValidSyntax(sourceCode);
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidSyntax_WithEmptyCode_ShouldReturnFalse()
    {
        var result = _parser.IsValidSyntax("");
        result.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        var parser = new SourcePawnParser();
        var disposing = () => parser.Dispose();
        disposing.Should().NotThrow();
    }

    [Fact]
    public void ParseSource_AfterDispose_ShouldThrowObjectDisposedException()
    {
        _parser.Dispose();
        var sourceCode = ReadTestCase("SourcePawnParser/ParsingTests/dispose_test_input.sp");
        var parsing = () => _parser.ParseSource(sourceCode);
        parsing.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _parser.Dispose();
    }
}
