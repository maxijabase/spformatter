using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class FailClosedTests : FormatterTestBase
{
    [Fact]
    public void Clean_trees_format_without_recovery_flag()
    {
        const string input = "void t()\n{\n    int x = 1 + 2;\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            AllowSyntaxRecovery = false
        });
        f.Format(input).Should().Contain("int x = 1 + 2;");
    }

    [Fact]
    public void Syntax_errors_fail_closed_by_default()
    {
        const string input = "void t( {";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(input);
        result.Success.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Format_throws_on_syntax_errors_by_default()
    {
        const string input = "void t( {";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        Action act = () => f.Format(input);
        act.Should().Throw<FormatException>();
    }
}
