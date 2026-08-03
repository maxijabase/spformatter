using Xunit;

namespace SpFormatter.Tests;

public class FunctagTests : FormatterTestBase
{
    [Theory]
    [InlineData("Functags/Basic/simple_functag")]
    [InlineData("Functags/Basic/functag_public_name")]
    public void Functags_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Functag_ShouldBeIdempotent()
    {
        var input = """
            functag SrvCmd Action:public(args);
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
