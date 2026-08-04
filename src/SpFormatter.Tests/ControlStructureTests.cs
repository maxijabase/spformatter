using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// Control structure goldens use exact match when an expected file exists.
/// </summary>
public class ControlStructureTests : FormatterTestBase
{
    [Theory]
    [InlineData("ControlStructures/SimpleIf/if_condition")]
    [InlineData("ControlStructures/SimpleIf/if_with_space")]
    [InlineData("ControlStructures/SimpleIf/if_extra_spaces")]
    [InlineData("ControlStructures/SimpleIf/if_condition_line_comments")]
    [InlineData("ControlStructures/SimpleIf/if_paren_trailing_line_comment")]
    public void SimpleIfStatements_ShouldFormatExactly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("ControlStructures/ForLoops/for_simple")]
    [InlineData("ControlStructures/ForLoops/for_no_spaces")]
    [InlineData("ControlStructures/ForLoops/for_old_multi_decl")]
    public void ForLoops_ShouldFormatExactly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Theory]
    [InlineData("ControlStructures/ReturnStatements/return_simple")]
    [InlineData("ControlStructures/ReturnStatements/return_with_value")]
    [InlineData("ControlStructures/ReturnStatements/return_expression")]
    [InlineData("ControlStructures/ReturnStatements/return_bare_then_assignment")]
    [InlineData("ControlStructures/ReturnStatements/return_multiline_ternary")]
    public void ReturnStatements_ShouldFormatExactly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void IfElseChain_ShouldFormatExactly()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/IfElseChain/if_else_chain");
    }

    [Fact]
    public void SwitchStatement_ShouldFormatExactly()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/SwitchStatements/switch_simple");
    }
}
