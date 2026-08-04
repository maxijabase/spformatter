using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class SourceFileLayoutTests : FormatterTestBase
{
    [Fact]
    public void Top_level_order_is_preserved_by_default()
    {
        const string input = """
            void Early()
            {
            }

            #include <sourcemod>

            int g_Value = 1;

            void Late()
            {
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.Format(input);
        var early = result.IndexOf("void Early()", StringComparison.Ordinal);
        var include = result.IndexOf("#include <sourcemod>", StringComparison.Ordinal);
        var global = result.IndexOf("int g_Value", StringComparison.Ordinal);
        var late = result.IndexOf("void Late()", StringComparison.Ordinal);
        early.Should().BeLessThan(include);
        include.Should().BeLessThan(global);
        global.Should().BeLessThan(late);
    }

    [Fact]
    public void PreserveEmptyLines_false_strips_blank_lines_from_output()
    {
        const string input = "#include <a>\n\n\nvoid t()\n{\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = false,
            NewLineAfterInclude = true
        });
        var result = f.Format(input);
        result.Should().NotContain("\n\n");
    }

    [Fact]
    public void MaxConsecutiveEmptyLines_caps_include_separator()
    {
        const string input = "#include <a>\n\n\n\nvoid t()\n{\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions
        {
            LineEnding = "\n",
            PreserveEmptyLines = true,
            MaxConsecutiveEmptyLines = 1,
            NewLineAfterInclude = true
        });
        var result = f.Format(input);
        result.Should().Contain("#include <a>\n\nvoid t()");
        result.Should().NotContain("\n\n\n");
    }
}
