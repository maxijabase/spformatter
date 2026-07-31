using Xunit;

namespace SpFormatter.Tests;

public class OldTypeTests : FormatterTestBase
{
    [Theory]
    [InlineData("Variables/OldType/old_type_colon")]
    public void OldType_ShouldKeepColonGlued(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void OldType_ShouldBeIdempotentAndParseClean()
    {
        var input = """
            new Float: b = 0.23;
            Handle:x;

            void Foo()
            {
                new Float: b = 0.23;
                Float: c = 1.0;
                int x = Float:0;
            }
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
        Assert.DoesNotContain(" : ", once);
        Assert.Contains("Float:b", once);
        Assert.Contains("Handle:x", once);
        Assert.Contains("Float:0", once);
    }
}
