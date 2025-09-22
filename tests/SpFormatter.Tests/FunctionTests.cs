using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// File-based tests for function declaration and call formatting
/// </summary>
public class FunctionTests : FormatterTestBase
{
    #region Function Declarations

    [Theory]
    [InlineData("Functions/SimpleDeclarations/simple_function")]
    [InlineData("Functions/SimpleDeclarations/simple_function_no_space")]
    public void SimpleFunctionDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Functions/WithParameters/function_with_params")]
    [InlineData("Functions/WithParameters/function_no_space_params")]
    public void FunctionDeclarationsWithParameters_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Functions/StockFunctions/stock_function")]
    [InlineData("Functions/StockFunctions/stock_default_param")]
    public void StockFunctionDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Functions/NativeDeclarations/native_function")]
    [InlineData("Functions/NativeDeclarations/native_function_spaces")]
    public void NativeDeclarations_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Syntax Validation Tests

    [Theory]
    [InlineData("Functions/SimpleDeclarations/simple_function")]
    [InlineData("Functions/WithParameters/function_with_params")]
    [InlineData("Functions/StockFunctions/stock_function")]
    [InlineData("Functions/NativeDeclarations/native_function")]
    public void FunctionTests_ShouldProduceValidSyntax(string testCaseName)
    {
        AssertTestCaseProducesValidSyntax(testCaseName);
    }

    #endregion
}