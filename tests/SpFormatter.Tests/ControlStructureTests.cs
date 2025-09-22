using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// File-based tests for control structure formatting that focus on syntax validity rather than exact string matching
/// </summary>
public class ControlStructureTests : FormatterTestBase
{
    [Theory]
    [InlineData("ControlStructures/SimpleIf/if_condition")]
    [InlineData("ControlStructures/SimpleIf/if_with_space")]
    [InlineData("ControlStructures/SimpleIf/if_extra_spaces")]
    public void SimpleIfStatements_ShouldProduceValidSyntax(string testCaseName)
    {
        AssertTestCaseProducesValidSyntax(testCaseName);
    }

    [Theory]
    [InlineData("ControlStructures/ForLoops/for_simple")]
    [InlineData("ControlStructures/ForLoops/for_no_spaces")]
    public void ForLoops_ShouldProduceValidSyntax(string testCaseName)
    {
        AssertTestCaseProducesValidSyntax(testCaseName);
    }

    [Theory]
    [InlineData("ControlStructures/ReturnStatements/return_simple")]
    [InlineData("ControlStructures/ReturnStatements/return_with_value")]
    [InlineData("ControlStructures/ReturnStatements/return_expression")]
    public void ReturnStatements_ShouldProduceValidSyntax(string testCaseName)
    {
        AssertTestCaseProducesValidSyntax(testCaseName);
    }

    [Fact]
    public void IfElseChain_ShouldProduceValidSyntax()
    {
        AssertTestCaseProducesValidSyntax("ControlStructures/IfElseChain/if_else_chain");
    }

    [Fact]
    public void SwitchStatement_ShouldProduceValidSyntax()
    {
        AssertTestCaseProducesValidSyntax("ControlStructures/SwitchStatements/switch_simple");
    }

    /// <summary>
    /// This test checks exact formatting for cases where we want to ensure specific behavior
    /// Only include tests here that are critical for correctness, not cosmetic formatting
    /// </summary>
    [Theory]
    [InlineData("ControlStructures/ReturnStatements/return_simple")]
    [InlineData("ControlStructures/ReturnStatements/return_with_value")]
    [InlineData("ControlStructures/ReturnStatements/return_expression")]
    public void ReturnStatements_ShouldFormatExactly(string testCaseName)
    {
        // Return statements should be preserved exactly as they're simple and unambiguous
        AssertTestCaseFormatsCorrectly(testCaseName);
    }
}