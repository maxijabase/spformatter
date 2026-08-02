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
    public void Switch_bare_case_body_matches_golden()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/SwitchStatements/switch_bare_case_body");
    }

    [Fact]
    public void Switch_preproc_around_cases_matches_golden()
    {
        AssertTestCaseFormatsCorrectly("ControlStructures/SwitchStatements/switch_preproc_cases");
    }

    [Fact]
    public void Bare_case_statement_body_is_idempotent()
    {
        const string input = """
            void TestFunction(int type)
            {
                switch(type)
                {
                    case 1: entity = CreateEntityByName("weapon_pistol");
                    case 2: entity = CreateEntityByName("weapon_pistol_magnum");
                    case 3: entity = CreateEntityByName("weapon_melee");
                }
            }
            """;
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var once = f.Format(input);
        once.Should().Contain("case 1:\n            entity = CreateEntityByName(\"weapon_pistol\");");
        f.Format(once).Should().Be(once);
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
