using FluentAssertions;
using SpFormatter;

namespace SpModernizer.Tests;

public class IdentityTests : ModernizerTestBase
{
    [Fact]
    public void Already_modern_source_is_unchanged()
    {
        const string input = """
float x = 5.0;
int y = 7;

public void OnPluginStart()
{
    char name[32];
}
""";
        AssertModernizeEquals(input, input);
    }

    [Fact]
    public void Modern_new_expression_is_preserved()
    {
        const string input = """
public void OnPluginStart()
{
    int[] players = new int[MaxClients + 1];
    ArrayList list = new ArrayList();
}
""";
        AssertModernizeEquals(input, input);
    }

    [Fact]
    public void FormatAfter_calls_formatter()
    {
        using var modernizer = new SourcePawnModernizer(new ModernizeOptions
        {
            FormatAfter = true,
            FormattingOptions = new SpFormatter.FormattingOptions { LineEnding = "\n" },
        });

        var result = modernizer.ModernizeWithResult("new Float:x=5.0;");
        result.Success.Should().BeTrue();
        result.Text.Should().Contain("float");
        result.Text.Should().NotContain("Float:");
    }

    [Fact]
    public void SpFormatter_still_preserves_old_tags()
    {
        using var formatter = new SpFormatter.SourcePawnFormatter(new SpFormatter.FormattingOptions
        {
            LineEnding = "\n",
        });
        var formatted = formatter.Format("new Float: b = 0.23;");
        formatted.Should().Contain("Float:");
    }
}
