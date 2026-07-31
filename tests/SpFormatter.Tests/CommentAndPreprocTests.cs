using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class CommentAndPreprocTests : FormatterTestBase
{
    [Fact]
    public void Line_comment_preserves_text_and_indent()
    {
        const string input = "void t()\n{\n    // keep me\n    return;\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("    // keep me");
    }

    [Fact]
    public void Block_comment_preserves_body_text()
    {
        const string input = "void t()\n{\n    /* hello\n     * world\n     */\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.Format(input);
        result.Should().Contain("/* hello");
        result.Should().Contain("world");
    }

    [Fact]
    public void Include_directive_text_is_preserved()
    {
        const string input = "#include <sourcemod>\n\nvoid t()\n{\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().StartWith("#include <sourcemod>");
    }

    [Fact]
    public void SortIncludes_false_preserves_include_order()
    {
        const string input = "#include <b>\n#include <a>\n\nvoid t()\n{\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SortIncludes = false
        });
        var result = f.Format(input);
        var b = result.IndexOf("#include <b>", StringComparison.Ordinal);
        var a = result.IndexOf("#include <a>", StringComparison.Ordinal);
        b.Should().BeGreaterThanOrEqualTo(0);
        a.Should().BeGreaterThan(b);
    }

    [Fact]
    public void SortIncludes_true_and_false_diverge_when_unsorted()
    {
        const string input = "#include <b>\n#include <a>\n\nvoid t()\n{\n}";
        using var unsorted = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SortIncludes = false
        });
        using var sorted = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SortIncludes = true
        });
        unsorted.Format(input).Should().NotBe(sorted.Format(input));
        sorted.Format(input).Should().MatchRegex("(?s)#include <a>.*#include <b>");
    }
}
