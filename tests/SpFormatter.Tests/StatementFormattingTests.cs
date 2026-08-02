using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class StatementFormattingTests : FormatterTestBase
{
    [Fact]
    public void Expression_statement_keeps_semicolon_when_present()
    {
        const string input = "void test()\n{\n    PrintToServer(\"hi\");\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("PrintToServer(\"hi\");");
    }

    [Fact]
    public void Expression_statement_adds_semicolon_when_required()
    {
        const string input = "void test()\n{\n    PrintToServer(\"hi\")\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            RequireSemicolons = true
        });
        f.Format(input).Should().Contain("PrintToServer(\"hi\");");
    }

    [Fact]
    public void Delete_keeps_space_before_indexed_operand()
    {
        AssertTestCaseFormatsCorrectly("Statements/Delete/delete_indexed");
    }

    [Fact]
    public void Break_and_continue_keep_semicolons()
    {
        const string input = """
            void test()
            {
                for (int i = 0; i < 10; i++)
                {
                    if (i == 1)
                    {
                        continue;
                    }
                    if (i == 5)
                    {
                        break;
                    }
                }
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.Format(input);
        result.Should().Contain("continue;");
        result.Should().Contain("break;");
    }

    [Theory]
    [InlineData("ControlStructures/ReturnStatements/return_simple")]
    [InlineData("ControlStructures/ReturnStatements/return_with_value")]
    [InlineData("ControlStructures/ReturnStatements/return_expression")]
    public void Return_statements_match_goldens(string testCase)
    {
        AssertTestCaseFormatsCorrectly(testCase);
    }

    [Fact]
    public void Bare_return_is_indented_with_semicolon()
    {
        const string input = "void test()\n{\nreturn\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("    return;");
    }
}
