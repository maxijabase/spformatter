using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class LoopFormattingTests : FormatterTestBase
{
    [Theory]
    [InlineData("ControlStructures/ForLoops/for_simple")]
    [InlineData("ControlStructures/ForLoops/for_no_spaces")]
    [InlineData("ControlStructures/ForLoops/for_old_multi_decl")]
    [InlineData("ControlStructures/ForPreproc/for_else_preproc")]
    [InlineData("ControlStructures/WhileLoops/while_trailing_line_comment")]
    [InlineData("ControlStructures/WhileLoops/while_legacy_do")]
    [InlineData("ControlStructures/DoWhile/do_while_trailing_comment")]
    [InlineData("ControlStructures/DoWhile/do_while_bare_condition")]
    public void For_loop_goldens_match(string testCase)
    {
        AssertTestCaseFormatsCorrectly(testCase);
    }

    [Fact]
    public void While_with_bool_condition_formats()
    {
        const string input = "void t()\n{\n    while(true)\n    {\n        break;\n    }\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.Format(input);
        result.Should().Contain("while(true)");
        result.Should().Contain("        break;");
    }

    [Fact]
    public void Bare_while_body_is_not_brace_wrapped()
    {
        const string input = "void t()\n{\n    while(x)\n        break;\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("while(x)\n        break;");
    }

    [Fact]
    public void Empty_for_header_keeps_semicolons()
    {
        const string input = "void t()\n{\n    for(;;)\n    {\n        break;\n    }\n}";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        f.Format(input).Should().Contain("for(;;)");
    }
}
