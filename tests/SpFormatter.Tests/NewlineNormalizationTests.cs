using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class NewlineNormalizationTests
{
    [Fact]
    public void Cr_only_source_with_http_url_formats_and_keeps_url_intact()
    {
        // Old Mac CR-only line endings; without normalization Tree-sitter sees one line
        // and splits `http://` into a string plus a `//` comment.
        var input = "#pragma semicolon 1\r\rpublic Plugin:myinfo =\r{\r    url = \"http://www.geek-gaming.fr\"\r}\r";
        using var f = new SourcePawnFormatter(new FormattingOptions { LineEnding = "\n" });
        var result = f.FormatWithResult(input);
        result.Success.Should().BeTrue();
        result.Text.Should().Contain("url = \"http://www.geek-gaming.fr\"");
        result.Text.Should().NotContain("url = \"http:\n");
    }
}
