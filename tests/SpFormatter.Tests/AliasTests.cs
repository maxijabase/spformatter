using Xunit;

namespace SpFormatter.Tests;

public class AliasTests : FormatterTestBase
{
    [Theory]
    [InlineData("Aliases/Basic/simple_alias")]
    public void Aliases_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Alias_ShouldBeIdempotent()
    {
        var input = """
            stock float operator++(float oper) {
                return oper + 1.0;
            }
            native float operator*(float a, float b) = FloatMul;
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
        Assert.Contains("operator++", once);
        Assert.Contains("operator*", once);
    }
}
