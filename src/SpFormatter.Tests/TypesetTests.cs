using Xunit;

namespace SpFormatter.Tests;

public class TypesetTests : FormatterTestBase
{
    [Theory]
    [InlineData("Typesets/Basic/simple_typeset")]
    public void Typesets_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Typeset_ShouldBeIdempotent()
    {
        var input = """
            typeset EventHook {
                function Action (Event event, const char[] name, bool dontBroadcast);
                function void (Event event, const char[] name, bool dontBroadcast);
            };
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
