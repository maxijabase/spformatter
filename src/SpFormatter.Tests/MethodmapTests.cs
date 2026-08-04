using Xunit;

namespace SpFormatter.Tests;

public class MethodmapTests : FormatterTestBase
{
    [Theory]
    [InlineData("Methodmaps/Basic/simple_methodmap")]
    [InlineData("Methodmaps/Basic/property_setter_alias")]
    public void Methodmaps_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void Methodmap_ShouldBeIdempotent()
    {
        var input = """
            methodmap AdtArray < Handle {
                public native int Length();
                property int Size {
                    public get() = GetArraySize;
                }
            };
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
