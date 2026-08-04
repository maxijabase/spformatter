using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class ArrayFormattingTests : FormatterTestBase
{
    [Theory]
    [InlineData("Variables/ArrayDeclarations/simple_array")]
    [InlineData("Variables/ArrayDeclarations/matrix_array")]
    [InlineData("Variables/ArrayDeclarations/complex_array")]
    public void Array_declarations_format_exactly(string testCase)
    {
        AssertTestCaseFormatsCorrectly(testCase);
    }

    [Fact]
    public void SpaceInArrayBrackets_true_and_false_diverge()
    {
        const string input = "void test()\n{\n    char buffer[256];\n}";

        var tight = new FormattingOptions { SpaceInArrayBrackets = false, LineEnding = "\n" };
        var spaced = new FormattingOptions { SpaceInArrayBrackets = true, LineEnding = "\n" };

        using var tightFormatter = new SourcePawnFormatter(tight);
        using var spacedFormatter = new SourcePawnFormatter(spaced);

        var tightOut = tightFormatter.Format(input);
        var spacedOut = spacedFormatter.Format(input);

        tightOut.Should().Contain("buffer[256]");
        spacedOut.Should().Contain("buffer[ 256 ]");
        tightOut.Should().NotBe(spacedOut);
    }

    [Fact]
    public void Array_access_option_fixtures_diverge()
    {
        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/array_access_false_input.sp",
            "FormattingOptions/SourcePawnSpecific/array_access_false_expected.sp",
            new FormattingOptions { SpaceInArrayBrackets = false });

        AssertFormatEqualsWithOptionsFromFiles(
            "FormattingOptions/SourcePawnSpecific/array_access_true_input.sp",
            "FormattingOptions/SourcePawnSpecific/array_access_true_expected.sp",
            new FormattingOptions { SpaceInArrayBrackets = true });
    }
}
