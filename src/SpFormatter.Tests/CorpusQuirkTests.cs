using Xunit;

namespace SpFormatter.Tests;

public class CorpusQuirkTests : FormatterTestBase
{
    [Theory]
    [InlineData("Literals/Chars/null_char")]
    [InlineData("ControlStructures/IfTrailingComment/if_trailing_comment")]
    [InlineData("Preprocessor/DefineUrl/define_http_url")]
    public void CorpusQuirks_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void CharLiteral_And_IfTrailingComment_ShouldBeIdempotent()
    {
        var input = """
            void F(int client)
            {
                char c = '\0';
                if (client == 0) // keep me
                {
                    return;
                }
                else
                {
                    return;
                }
            }
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
        Assert.Contains("'\\0'", once);
        Assert.DoesNotContain("' \\0 '", once);
        Assert.Contains("// keep me", once);
        Assert.DoesNotContain("// keep me;", once);
    }
}
