using Xunit;

namespace SpFormatter.Tests;

public class TypedefTests : FormatterTestBase
{
    [Theory]
    [InlineData("Typedefs/Basic/simple_typedef")]
    public void Typedefs_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Typedef_ShouldBeIdempotent()
    {
        var input = """
            typedef SQLTxnFailure = function void (Database db, any data);
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
