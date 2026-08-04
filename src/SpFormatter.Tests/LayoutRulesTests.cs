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
    public void JoinDeclarationParts_spaces_words_and_commas()
    {
        var rules = new LayoutRules(new FormattingOptions { SpaceAfterComma = true, SpaceAroundOperators = true });
        rules.JoinDeclarationParts(new[] { "int", "a", ",", "b" }).Should().Be("int a, b");
        rules.JoinDeclarationParts(new[] { "int", "x", " = ", "5" }).Should().Be("int x = 5");
        rules.JoinDeclarationParts(new[] { "char", "buffer", "[256]" }).Should().Be("char buffer[256]");
    }

    [Fact]
    public void JoinDeclarationParts_respects_no_space_after_comma()
    {
        var rules = new LayoutRules(new FormattingOptions { SpaceAfterComma = false });
        rules.JoinDeclarationParts(new[] { "int", "a", ",", "b" }).Should().Be("int a,b");
    }

    [Fact]
    public void JoinDeclarationParts_keeps_old_type_colon_glued()
    {
        var rules = new LayoutRules(FormattingOptions.Default);
        rules.JoinDeclarationParts(new[] { "Handle:", "x" }).Should().Be("Handle:x");
        rules.JoinDeclarationParts(new[] { "Action:", "public", "(args)" }).Should().Be("Action:public(args)");
    }

    [Fact]
    public void ControlParenSpace_is_always_on()
    {
        var off = new LayoutRules(new FormattingOptions { SpaceBeforeOpenParen = false });
        off.ControlParenSpace.Should().Be(" ");

        var on = new LayoutRules(new FormattingOptions { SpaceBeforeOpenParen = true });
        on.ControlParenSpace.Should().Be(" ");
    }

    [Fact]
    public void CallWithParen_respects_space_before_open_paren()
    {
        var tight = new LayoutRules(new FormattingOptions { SpaceBeforeOpenParen = false });
        tight.CallWithParen("PrintToChat", "client, msg").Should().Be("PrintToChat(client, msg)");

        var spaced = new LayoutRules(new FormattingOptions { SpaceBeforeOpenParen = true });
        spaced.CallWithParen("PrintToChat", "client, msg").Should().Be("PrintToChat (client, msg)");
    }

    [Fact]
    public void CountBlankLinesInGap_basic_cases()
    {
        LayoutRules.CountBlankLinesInGap("\n".AsSpan()).Should().Be(0);
        LayoutRules.CountBlankLinesInGap("\n\n".AsSpan()).Should().Be(1);
        LayoutRules.CountBlankLinesInGap("\n\n\n".AsSpan()).Should().Be(2);
    }
}
