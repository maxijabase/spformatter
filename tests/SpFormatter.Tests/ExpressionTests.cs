using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// File-based tests for expression and operator formatting
/// </summary>
public class ExpressionTests : FormatterTestBase
{
    #region Binary Operators

    [Theory]
    [InlineData("Expressions/BinaryOperators/arithmetic_addition")]
    [InlineData("Expressions/BinaryOperators/arithmetic_multiplication")]
    [InlineData("Expressions/BinaryOperators/arithmetic_division")]
    [InlineData("Expressions/BinaryOperators/arithmetic_modulo")]
    public void ArithmeticOperators_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Expressions/BinaryOperators/comparison_equals")]
    [InlineData("Expressions/BinaryOperators/comparison_not_equals")]
    [InlineData("Expressions/BinaryOperators/comparison_less_than")]
    [InlineData("Expressions/BinaryOperators/comparison_less_equals")]
    [InlineData("Expressions/BinaryOperators/comparison_greater_equals")]
    public void ComparisonOperators_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("Expressions/BinaryOperators/logical_and")]
    [InlineData("Expressions/BinaryOperators/logical_or")]
    [InlineData("Expressions/BinaryOperators/logical_not")]
    public void LogicalOperators_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Assignment Operators

    [Theory]
    [InlineData("Expressions/AssignmentOperators/basic_assignment")]
    [InlineData("Expressions/AssignmentOperators/compound_add")]
    [InlineData("Expressions/AssignmentOperators/compound_subtract")]
    [InlineData("Expressions/AssignmentOperators/chained_assignment")]
    public void AssignmentOperators_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Update Expressions

    [Theory]
    [InlineData("Expressions/UpdateExpressions/post_increment")]
    [InlineData("Expressions/UpdateExpressions/pre_increment")]
    [InlineData("Expressions/UpdateExpressions/post_decrement")]
    [InlineData("Expressions/UpdateExpressions/pre_decrement")]
    public void UpdateExpressions_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Array Access

    [Theory]
    [InlineData("Expressions/ArrayAccess/simple_array_access")]
    [InlineData("Expressions/ArrayAccess/matrix_access")]
    [InlineData("Expressions/ArrayAccess/array_expression_access")]
    public void ArrayAccess_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Function Call Expressions

    [Theory]
    [InlineData("Expressions/FunctionCalls/simple_function_call")]
    [InlineData("Expressions/FunctionCalls/function_with_params")]
    [InlineData("Expressions/FunctionCalls/nested_function_call")]
    public void FunctionCallExpressions_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Complex Expressions

    [Theory]
    [InlineData("Expressions/ComplexExpressions/mathematical_expression")]
    [InlineData("Expressions/ComplexExpressions/logical_expression")]
    [InlineData("Expressions/ComplexExpressions/assignment_expression")]
    public void ComplexExpressions_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    #endregion

    #region Syntax Validation Tests

    [Theory]
    [InlineData("Expressions/BinaryOperators/arithmetic_addition")]
    [InlineData("Expressions/BinaryOperators/comparison_equals")]
    [InlineData("Expressions/BinaryOperators/logical_and")]
    [InlineData("Expressions/AssignmentOperators/basic_assignment")]
    [InlineData("Expressions/UpdateExpressions/post_increment")]
    [InlineData("Expressions/ArrayAccess/simple_array_access")]
    [InlineData("Expressions/FunctionCalls/simple_function_call")]
    [InlineData("Expressions/ComplexExpressions/mathematical_expression")]
    public void ExpressionTests_ShouldProduceValidSyntax(string testCaseName)
    {
        AssertTestCaseProducesValidSyntax(testCaseName);
    }

    #endregion
}