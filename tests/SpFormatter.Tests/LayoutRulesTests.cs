using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class LayoutRulesTests
{
    [Fact]
    public void JoinComma_respects_space_after_comma()
    {
        var withSpace = new LayoutRules(new FormattingOptions { SpaceAfterComma = true });
        withSpace.JoinComma(new[] { "a", "b" }).Should().Be("a, b");

        var without = new LayoutRules(new FormattingOptions { SpaceAfterComma = false });
        without.JoinComma(new[] { "a", "b" }).Should().Be("a,b");
    }

    [Fact]
    public void FormatBinaryOperator_respects_space_around_operators()
    {
        var spaced = new LayoutRules(new FormattingOptions { SpaceAroundOperators = true });
        spaced.FormatBinaryOperator("+").Should().Be(" + ");

        var tight = new LayoutRules(new FormattingOptions { SpaceAroundOperators = false });
        tight.FormatBinaryOperator("+").Should().Be("+");
    }

    [Fact]
    public void Indent_uses_spaces_or_tabs()
    {
        var spaces = new LayoutRules(new FormattingOptions { IndentSize = 2, UseSpaces = true });
        spaces.Indent(2).Should().Be("    ");

        var tabs = new LayoutRules(new FormattingOptions { UseSpaces = false });
        tabs.Indent(2).Should().Be("\t\t");
    }

    [Fact]
    public void ArrayAccess_respects_bracket_spacing()
    {
        var tight = new LayoutRules(new FormattingOptions { SpaceInArrayBrackets = false });
        tight.ArrayAccess("buf", "0").Should().Be("buf[0]");

        var spaced = new LayoutRules(new FormattingOptions { SpaceInArrayBrackets = true });
        spaced.ArrayAccess("buf", "0").Should().Be("buf[ 0 ]");
    }
}
