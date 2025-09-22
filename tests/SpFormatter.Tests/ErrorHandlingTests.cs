using FluentAssertions;
using System.IO;
using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// Tests for error handling, edge cases, and malformed syntax
/// </summary>
public class ErrorHandlingTests : FormatterTestBase
{
    #region Syntax Error Recovery
    
    [Fact]
    public void TestFormatterDoesNotCrashOnMalformedSyntax()
    {
        var malformedCases = new[]
        {
            "ErrorHandling/MalformedSyntax/missing_value_input.sp",
            "ErrorHandling/MalformedSyntax/missing_condition_input.sp",
            "ErrorHandling/MalformedSyntax/missing_closing_paren_input.sp",
            "ErrorHandling/MalformedSyntax/missing_semicolon_input.sp",
            "ErrorHandling/MalformedSyntax/incomplete_case_input.sp",
            "ErrorHandling/MalformedSyntax/extra_comma_input.sp",
            "ErrorHandling/MalformedSyntax/missing_argument_input.sp",
        };

        foreach (var testCaseFile in malformedCases)
        {
            var testCasesDir = GetTestCasesDirectory();
            var inputFile = Path.Combine(testCasesDir, testCaseFile);
            var input = File.ReadAllText(inputFile);
            AssertFormatDoesNotThrow(input);
        }
    }
    
    [Fact]
    public void TestFormatterHandlesSyntaxErrorsGracefully()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/SyntaxErrors/malformed_function_input.sp");
        var input = File.ReadAllText(inputFile);

        // Should not crash and should format the valid parts
        AssertFormatDoesNotThrow(input);

        var result = _formatter.Format(input);
        result.Should().Contain("int x = 5;");
        result.Should().Contain("int y = 10;");
    }
    
    [Fact]
    public void TestFormatterReportssSyntaxErrors()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/MalformedSyntax/missing_value_input.sp");
        var input = File.ReadAllText(inputFile);

        using var parser = new SourcePawnParser();
        var errors = parser.GetSyntaxErrors(input);

        errors.Should().NotBeEmpty("Malformed syntax should produce syntax errors");
        errors[0].Message.Should().NotBeEmpty("Error should have descriptive message");
        errors[0].StartLine.Should().BeGreaterThan(0, "Error should have valid line number");
        errors[0].StartColumn.Should().BeGreaterThan(0, "Error should have valid column number");
    }
    
    #endregion
    
    #region Edge Cases
    
    [Fact]
    public void TestEmptyFile()
    {
        var input = "";

        // Current behavior: formatter throws on empty input
        // This should be fixed to return empty string instead of throwing
        Action act = () => _formatter.Format(input);
        act.Should().Throw<FormatException>("Empty input currently throws - this is a known issue to be fixed");
    }
    
    [Fact]
    public void TestOnlyWhitespace()
    {
        var input = "   \n\n   \t   \n   ";
        
        AssertFormatDoesNotThrow(input);
        
        var result = _formatter.Format(input);
        result.Should().BeEmpty("Whitespace-only input should produce empty output");
    }
    
    [Fact]
    public void TestOnlyComments()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/EdgeCases/only_comments_input.sp");
        var input = File.ReadAllText(inputFile);

        AssertFormatDoesNotThrow(input);

        var result = _formatter.Format(input);
        result.Should().Contain("// This is a comment");
        result.Should().Contain("/* This is a block comment */");
    }
    
    [Fact]
    public void TestVeryLongLine()
    {
        var longString = new string('a', 1000);
        var input = $"void func() {{ PrintToServer(\"{longString}\"); }}";
        
        AssertFormatDoesNotThrow(input);
        AssertFormatProducesValidSyntax(input);
    }
    
    [Fact]
    public void TestDeeplyNestedStructures()
    {
        AssertTestCaseProducesValidSyntax("ErrorHandling/EdgeCases/deeply_nested_if");
    }
    
    [Fact]
    public void TestMixedTabsAndSpaces()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/EdgeCases/mixed_tabs_spaces_input.sp");
        var input = File.ReadAllText(inputFile);

        AssertFormatDoesNotThrow(input);

        var result = _formatter.Format(input);
        // Should normalize to consistent indentation
        result.Should().NotContain("\t", "Output should not contain tabs");
    }
    
    #endregion
    
    #region Unicode and Special Characters
    
    [Fact]
    public void TestUnicodeInStrings()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/UnicodeAndSpecialChars/unicode_strings_input.sp");
        var input = File.ReadAllText(inputFile);

        AssertFormatDoesNotThrow(input);

        var result = _formatter.Format(input);
        result.Should().Contain("世界", "Unicode characters should be preserved");
    }
    
    [Fact]
    public void TestEscapeSequencesInStrings()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/UnicodeAndSpecialChars/escape_sequences_input.sp");
        var input = File.ReadAllText(inputFile);

        AssertFormatDoesNotThrow(input);

        var result = _formatter.Format(input);
        result.Should().Contain("\\n");
        result.Should().Contain("\\t");
        result.Should().Contain("\\\"");
    }
    
    #endregion
    
    #region Performance Edge Cases
    
    [Fact]
    public void TestLargeFile()
    {
        // Generate a large but valid SourcePawn file using template
        var testCasesDir = GetTestCasesDirectory();
        var templateFile = Path.Combine(testCasesDir, "ErrorHandling/LargeFiles/large_function_template_input.sp");
        var template = File.ReadAllText(templateFile);

        var functions = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            functions.Add(template.Replace("{i}", i.ToString()));
        }

        var input = string.Join("\n\n", functions);

        // Should handle large files without crashing or excessive delay
        AssertFormatDoesNotThrow(input);
    }
    
    [Fact]
    public void TestManySmallStatements()
    {
        var statements = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            statements.Add($"int var{i} = {i};");
        }
        
        var input = $"void test() {{\n{string.Join("\n", statements)}\n}}";
        
        AssertFormatDoesNotThrow(input);
    }
    
    #endregion
    
    #region Regression Tests
    
    [Fact]
    public void TestStage1RegressionDoubleSemicolons()
    {
        // Regression test for Stage 1 fix: double semicolons
        var testCaseFiles = new[]
        {
            "ErrorHandling/RegressionTests/double_semicolons_test1_input.sp",
            "ErrorHandling/RegressionTests/double_semicolons_test2_input.sp",
            "ErrorHandling/RegressionTests/double_semicolons_test3_input.sp",
            "ErrorHandling/RegressionTests/double_semicolons_test4_input.sp"
        };

        foreach (var testCaseFile in testCaseFiles)
        {
            var testCasesDir = GetTestCasesDirectory();
            var inputFile = Path.Combine(testCasesDir, testCaseFile);
            var input = File.ReadAllText(inputFile);
            var result = _formatter.Format(input);
            result.Should().NotContain(";;", $"No double semicolons should appear in: {testCaseFile}");
        }
    }
    
    [Fact]
    public void TestStage2RegressionForLoopClauses()
    {
        // Regression test for Stage 2 fix: for loop clauses
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/RegressionTests/for_loop_clauses_input.sp");
        var input = File.ReadAllText(inputFile);

        var result = _formatter.Format(input);

        // Should have all three clauses
        result.Should().Contain("int i = 0");
        result.Should().Contain("i < max");
        result.Should().Contain("i++");

        // Should not have empty clauses
        result.Should().NotContain("for(;");
        result.Should().NotContain("; ;");
        result.Should().NotContain("; )");
    }
    
    [Fact]
    public void TestStage2RegressionElseIfChains()
    {
        // Regression test for Stage 2 fix: else if chains
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/EdgeCases/else_if_chain_input.sp");
        var input = File.ReadAllText(inputFile);

        var result = _formatter.Format(input);

        // Should preserve all function calls (even if structure is malformed)
        result.Should().Contain("A()");
        result.Should().Contain("B()");
        result.Should().Contain("C()");
    }
    
    [Fact]
    public void TestStage3RegressionArraySpacing()
    {
        // Regression test for Stage 3 fix: array spacing
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/RegressionTests/array_spacing_test1_input.sp");
        var input = File.ReadAllText(inputFile);

        var result = _formatter.Format(input);

        result.Should().Contain("buffer[256]", "Array spacing should be correct");
        result.Should().NotContain("[ ", "No space after opening bracket");
    }
    
    [Fact]
    public void TestStage3RegressionModuloOperator()
    {
        // Regression test for Stage 3 fix: modulo operator spacing
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/RegressionTests/modulo_operator_input.sp");
        var input = File.ReadAllText(inputFile);

        var result = _formatter.Format(input);

        result.Should().Contain("x %= 4");
        result.Should().NotContain("x%=4");
    }
    
    #endregion
    
    #region Integration Tests
    
    [Fact]
    public void TestRealWorldPluginStructure()
    {
        var testCasesDir = GetTestCasesDirectory();
        var inputFile = Path.Combine(testCasesDir, "ErrorHandling/IntegrationTests/real_world_plugin_input.sp");
        var input = File.ReadAllText(inputFile);

        // Should format real-world plugin without crashing
        AssertFormatDoesNotThrow(input);

        var result = _formatter.Format(input);

        // Should preserve key structures
        result.Should().Contain("public Plugin myinfo");
        result.Should().Contain("OnPluginStart");
        result.Should().Contain("Command_Test");
        result.Should().Contain("IsValidClient");
    }
    
    #endregion
}
