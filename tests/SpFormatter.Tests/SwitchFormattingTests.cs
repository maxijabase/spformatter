using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class SwitchFormattingTests : FormatterTestBase
{
    [Fact]
    public void Switch_simple_matches_golden()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/SwitchStatements/switch_simple");
    }

    [Fact]
    public void Multi_value_case_keeps_comma_spacing()
    {
        const string input = """
            void t()
            {
                switch(v)
                {
                    case 2, 3:
                    {
                        break;
                    }
                }
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("case 2, 3:");
    }
}
