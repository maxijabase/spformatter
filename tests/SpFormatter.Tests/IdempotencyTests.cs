using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

public class IdempotencyTests : FormatterTestBase
{
    public static IEnumerable<object[]> StableGoldenInputs()
    {
        var roots = new[] { "Expressions", "Functions", "Variables", "ControlStructures" };
        var testCasesDir = GetStaticTestCasesDirectory();

        foreach (var root in roots)
        {
            var dir = Path.Combine(testCasesDir, root);
            if (!Directory.Exists(dir))
                continue;

            foreach (var inputPath in Directory.GetFiles(dir, "*_input.sp", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(testCasesDir, inputPath).Replace('\\', '/');
                yield return new object[] { relative };
            }
        }
    }

    [Theory]
    [MemberData(nameof(StableGoldenInputs))]
    public void Format_twice_matches_format_once(string relativeInputPath)
    {
        var fullPath = Path.Combine(GetTestCasesDirectory(), relativeInputPath);
        var input = NormalizeNewlines(File.ReadAllText(fullPath));

        string once;
        try
        {
            once = _formatter.Format(input);
        }
        catch (FormatException)
        {
            return;
        }

        var twice = _formatter.Format(once);
        twice.Should().Be(once, $"formatting '{relativeInputPath}' twice should be stable");
    }

    private static string GetStaticTestCasesDirectory()
    {
        var assemblyLocation = typeof(IdempotencyTests).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(assemblyDir, "TestCases");
    }
}
