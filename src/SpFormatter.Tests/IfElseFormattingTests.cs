using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class IfElseFormattingTests : FormatterTestBase
{
    [Theory]
    [InlineData("ControlStructures/SimpleIf/if_condition")]
    [InlineData("ControlStructures/SimpleIf/if_with_space")]
    [InlineData("ControlStructures/SimpleIf/if_extra_spaces")]
    public void Braced_if_goldens_match(string testCase)
    {
        AssertTestCaseFormatsCorrectly(testCase);
    }

    [Fact]
    public void If_else_chain_matches_golden()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/IfElseChain/if_else_chain");
    }

    [Fact]
    public void If_with_else_preproc_keeps_both_branches_and_body()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/IfPreproc/if_else_preproc");
    }

    [Fact]
    public void Bare_single_statement_if_is_not_brace_wrapped()
    {
        const string input = """
            void TestComparison()
            {
                if(x == 5)
                    return true;
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.Format(input);
        result.Should().Contain("if (x == 5)\n        return true;");
        result.Should().NotContain("if (x == 5)\n    {\n        return true;");
    }

    [Fact]
    public void Bool_literal_conditions_are_preserved()
    {
        const string input = "void t()\n{\n    if(true)\n    {\n        return;\n    }\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("if (true)");
    }

    [Fact]
    public void Bare_else_statement_is_not_brace_wrapped()
    {
        const string input = """
            void t()
            {
                if(a)
                    return 1;
                else
                    return 2;
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.Format(input);
        result.Should().Contain("else\n        return 2;");
        result.Should().NotContain("else\n    {");
    }
}
