using Xunit;

namespace SpFormatter.Tests;

public class StructTests : FormatterTestBase
{
    [Theory]
    [InlineData("Structs/Basic/simple_struct")]
    [InlineData("Structs/Basic/struct_field_comments")]
    [InlineData("Structs/Basic/struct_preproc_fields")]
    [InlineData("Structs/Basic/struct_trailing_comment")]
    [InlineData("Structs/Basic/struct_mid_comment")]
    [InlineData("Structs/Basic/struct_comma_fields")]
    public void Structs_ShouldFormatCorrectly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    [Fact]
    public void StructDeclaration_ShouldBeIdempotent()
    {
        var input = """
            public Plugin myinfo = { name = "Test", author = "Author" };
            """;
        AssertFormatProducesValidSyntax(input);
        var once = _formatter.Format(input);
        var twice = _formatter.Format(once);
        Assert.Equal(once, twice);
    }
}
