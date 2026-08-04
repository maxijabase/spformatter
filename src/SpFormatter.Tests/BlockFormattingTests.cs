using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class BlockFormattingTests : FormatterTestBase
{
    [Theory]
    [InlineData("Functions/SimpleDeclarations/simple_function")]
    [InlineData("Functions/WithParameters/function_with_params")]
    [InlineData("Functions/StockFunctions/stock_function")]
    public void Function_bodies_format_via_ast_blocks(string testCase)
    {
        AssertTestCaseFormatsCorrectly(testCase);
    }

    [Fact]
    public void Empty_block_keeps_braces_on_separate_lines()
    {
        const string input = "void test()\n{\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Be("void test()\n{\n}");
    }

    [Fact]
    public void Compact_option_keeps_body_on_same_line()
    {
        const string input = "void test()\n{\n    int x = 1;\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            NewLineAfterOpenBrace = false,
            LineEnding = "\n",
            SpaceAroundOperators = true
        });

        f.Format(input).Should().Be("void test() { int x = 1; }");
    }

    [Fact]
    public void Multiline_and_compact_block_styles_diverge()
    {
        const string input = "void test()\n{\n    return;\n}";
        using var multi = new SourcePawnFormatter(new FormattingOptions
        {
            NewLineAfterOpenBrace = true,
            LineEnding = "\n"
        });
        using var compact = new SourcePawnFormatter(new FormattingOptions
        {
            NewLineAfterOpenBrace = false,
            LineEnding = "\n"
        });

        var multiOut = multi.Format(input);
        var compactOut = compact.Format(input);

        multiOut.Should().NotBe(compactOut);
        multiOut.Should().Contain("\n{\n");
        compactOut.Should().Contain("{ return; }");
    }

    [Fact]
    public void Nested_block_indents_inner_statements()
    {
        const string input = "void test()\n{\n    {\n        int x = 1;\n    }\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            SpaceAroundOperators = true
        });
        var result = f.Format(input);
        result.Should().Contain("    {\n        int x = 1;\n    }");
    }
}
