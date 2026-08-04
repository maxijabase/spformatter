using Xunit;

namespace SpFormatter.Tests;

public class EnumTests : FormatterTestBase
{
    [Theory]
    [InlineData("Enums/Basic/simple_enum")]
    public void Enums_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Enum_ShouldBeIdempotent()
    {
        var input = """
            enum Foo(<<= 1) {
                A = 1,
                B,
            };
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
