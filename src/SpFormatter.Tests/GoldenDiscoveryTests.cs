using FluentAssertions;
using Xunit;

namespace SpFormatter.Tests;

/// <summary>
/// Auto-discovers exact-match goldens under TestCases categories that are expected to be stable.
/// Drop a pair of *_input.sp / *_expected.sp files to add coverage without editing C#.
/// </summary>
public class GoldenDiscoveryTests : FormatterTestBase
{
    public static IEnumerable<object[]> ExactMatchPairs()
    {
        var roots = new[]
        {
            "Expressions",
            "Functions",
            "Variables"
        };

        var testCasesDir = GetStaticTestCasesDirectory();
        foreach (var root in roots)
        {
            var dir = Path.Combine(testCasesDir, root);
            if (!Directory.Exists(dir))
                continue;

            foreach (var inputPath in Directory.GetFiles(dir, "*_input.sp", SearchOption.AllDirectories))
            {
                var expectedPath = inputPath.Replace("_input.sp", "_expected.sp");
                if (!File.Exists(expectedPath))
                    continue;

                var relative = Path.GetRelativePath(testCasesDir, inputPath)
                    .Replace('\\', '/')
                    .Replace("_input.sp", "");
                yield return new object[] { relative };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ExactMatchPairs))]
    public void Discovered_goldens_format_exactly(string testCaseName)
    {
        AssertTestCaseFormatsCorrectly(testCaseName);
    }

    private static string GetStaticTestCasesDirectory()
    {
        var assemblyLocation = typeof(GoldenDiscoveryTests).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(assemblyDir, "TestCases");
    }
}
