using Xunit;

namespace SpFormatter.Tests;

public class EnumStructTests : FormatterTestBase
{
    [Theory]
    [InlineData("EnumStructs/Basic/simple_enum_struct")]
    public void EnumStructs_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void EnumStruct_ShouldBeIdempotent()
    {
        var input = """
            enum struct Point {
                int x;
                int y;
                void Reset() {
                    this.x = 0;
                    this.y = 0;
                }
            }
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
