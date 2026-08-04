using FluentAssertions;
using SpFormatter;

namespace SpModernizer.Tests;

public abstract class ModernizerTestBase : IDisposable
{
    protected readonly SourcePawnModernizer Modernizer;

    protected ModernizerTestBase()
    {
        Modernizer = new SourcePawnModernizer(new ModernizeOptions
        {
            FormatAfter = false,
        });
    }

    protected void AssertModernizeEquals(string input, string expected)
    {
        var result = Modernizer.ModernizeWithResult(input);
        result.Success.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        Normalize(result.Text).Should().Be(Normalize(expected));
    }

    protected void AssertModernizeTestCase(string relativeNameWithoutSuffix)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "TestCases");
        var inputPath = Path.Combine(dir, relativeNameWithoutSuffix + "_input.sp");
        var expectedPath = Path.Combine(dir, relativeNameWithoutSuffix + "_expected.sp");
        File.Exists(inputPath).Should().BeTrue(inputPath);
        File.Exists(expectedPath).Should().BeTrue(expectedPath);
        AssertModernizeEquals(File.ReadAllText(inputPath), File.ReadAllText(expectedPath));
    }

    protected void AssertIdempotent(string input)
    {
        var once = Modernizer.Modernize(input);
        var twice = Modernizer.Modernize(once);
        Normalize(twice).Should().Be(Normalize(once));
    }

    protected static string Normalize(string text) =>
        text.Replace("\r\n", "\n").Replace("\r", "\n").TrimEnd() + "\n";

    public void Dispose() => Modernizer.Dispose();
}
