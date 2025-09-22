using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// File-based tests for variable declaration formatting
/// </summary>
public class VariableDeclarationTests : FormatterTestBase
{
    [Theory]
    [InlineData("Variables/SimpleDeclarations/simple_variable")]
    [InlineData("Variables/SimpleDeclarations/simple_variable_spaces")]
    public void SimpleVariableDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Variables/GlobalDeclarations/global_handle")]
    [InlineData("Variables/GlobalDeclarations/global_bool")]
    public void GlobalVariableDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Variables/MultipleDeclarations/multiple_vars")]
    [InlineData("Variables/MultipleDeclarations/multiple_vars_spaces")]
    public void MultipleVariableDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Variables/ArrayDeclarations/simple_array")]
    [InlineData("Variables/ArrayDeclarations/matrix_array")]
    [InlineData("Variables/ArrayDeclarations/complex_array")]
    public void ArrayDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #region Syntax Validation Tests

    [Theory]
    [InlineData("Variables/SimpleDeclarations/simple_variable")]
    [InlineData("Variables/GlobalDeclarations/global_handle")]
    [InlineData("Variables/MultipleDeclarations/multiple_vars")]
    [InlineData("Variables/ArrayDeclarations/simple_array")]
    [InlineData("Variables/ArrayDeclarations/complex_array")]
    public void VariableTests_ShouldProduceValidSyntax(string testCaseName)
    {
        AssertTestCaseProducesValidSyntax(testCaseName);
    }

    #endregion
}