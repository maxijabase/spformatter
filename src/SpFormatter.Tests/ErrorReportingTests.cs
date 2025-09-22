using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class ErrorReportingTests : IDisposable
{
    private readonly SourcePawnParser _parser;

    public ErrorReportingTests()
    {
        _parser = new SourcePawnParser();
    }

    [Fact]
    public void GetSyntaxErrors_WithValidCode_ShouldReturnEmptyList()
    {
        var code = "public void TestFunction() { int x = 5; }";
        
        var errors = _parser.GetSyntaxErrors(code);
        
        errors.Should().BeEmpty();
    }

    [Fact]
    public void GetSyntaxErrors_WithMissingSemicolon_ShouldReportError()
    {
        var code = @"public void TestFunction() {
    int x = 5
    int y = 10;
}";
        
        var errors = _parser.GetSyntaxErrors(code);
        
        errors.Should().NotBeEmpty();
        errors.Should().ContainSingle();
        
        var error = errors.First();
        error.StartLine.Should().Be(2); // Line with missing semicolon
        error.Message.Should().Contain("error");
        error.Context.Should().NotBeEmpty();
    }

    [Fact]
    public void GetSyntaxErrors_WithMissingParenthesis_ShouldReportError()
    {
        var code = @"public void TestFunction() {
    if (x > 5 {
        PrintToServer(""test"");
    }
}";
        
        var errors = _parser.GetSyntaxErrors(code);
        
        errors.Should().NotBeEmpty();
        var error = errors.First();
        error.StartLine.Should().Be(2); // Line with missing parenthesis
        error.Context.Should().Contain(">>>");
    }

    [Fact]
    public void SyntaxError_ToString_ShouldProvideReadableFormat()
    {
        var error = new SyntaxError
        {
            Message = "Test error",
            StartLine = 5,
            StartColumn = 10,
            NodeType = "identifier",
            IsMissing = false
        };
        
        var result = error.ToString();
        
        result.Should().Contain("Line 5:10");
        result.Should().Contain("Test error");
        result.Should().Contain("[Error]");
    }

    [Fact]
    public void SyntaxError_GetDetailedDescription_ShouldIncludeAllInfo()
    {
        var error = new SyntaxError
        {
            Message = "Test error",
            StartLine = 5,
            StartColumn = 10,
            EndLine = 5,
            EndColumn = 15,
            StartIndex = 50,
            EndIndex = 55,
            NodeType = "identifier",
            Context = "001| public void Test()\n>>> 002|     int x = error_here\n003|     return;"
        };
        
        var result = error.GetDetailedDescription();
        
        result.Should().Contain("Line 5:10");
        result.Should().Contain("Character Index: 50 to 55");
        result.Should().Contain("Node Type: identifier");
        result.Should().Contain(">>>");
    }

    public void Dispose()
    {
        _parser.Dispose();
    }
}
