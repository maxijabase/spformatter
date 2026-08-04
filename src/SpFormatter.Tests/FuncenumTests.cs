using Xunit;

namespace SpFormatter.Tests;

public class FuncenumTests : FormatterTestBase
{
    [Theory]
    [InlineData("Funcenums/Basic/simple_funcenum")]
    public void Funcenums_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Funcenum_ShouldBeIdempotent()
    {
        var input = """
            funcenum Timer {
                Action:public(Handle:timer, Handle:hndl),
                Action:public(Handle:timer),
            };
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
