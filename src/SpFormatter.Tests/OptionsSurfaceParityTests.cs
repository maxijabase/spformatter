using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace SpFormatter.Tests;

/// <summary>
/// Guards the vertical-slice rule: every honored FormattingOptions property must stay
/// listed in the catalog and present on CLI help, playground DTO, and desktop UI wiring.
/// </summary>
public class OptionsSurfaceParityTests
{
    private static readonly HashSet<string> IgnoredEngineProperties =
    [
        nameof(FormattingOptions.IndentString),
    ];

    [Fact]
    public void Catalog_matches_settable_FormattingOptions_properties()
    {
        var engineProps = typeof(FormattingOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.CanWrite)
            .Select(p => p.Name)
            .Where(name => !IgnoredEngineProperties.Contains(name))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        var catalogProps = FormattingOptionsCatalog.PropertyNames
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        catalogProps.Should().Equal(engineProps);
    }

    [Fact]
    public void Playground_FormatOptionsDto_exposes_every_catalog_property()
    {
        var dtoSource = ReadRepoFile("src/SpFormatter.Playground/Models/FormatRequest.cs");
        foreach (var name in FormattingOptionsCatalog.PropertyNames)
        {
            Regex.IsMatch(
                    dtoSource,
                    $@"public\s+\w+\??\s+{Regex.Escape(name)}\s*\{{")
                .Should().BeTrue($"FormatOptionsDto must declare {name}");
        }
    }

    [Fact]
    public void Playground_html_has_a_control_id_for_every_catalog_property()
    {
        var html = ReadRepoFile("src/SpFormatter.Playground/wwwroot/index.html");
        var expectedIds = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["IndentSize"] = "indentSize",
            ["UseSpaces"] = "useSpaces",
            ["SpaceAfterComma"] = "spaceAfterComma",
            ["SpaceAroundOperators"] = "spaceAroundOperators",
            ["SpaceBeforeOpenParen"] = "spaceBeforeOpenParen",
            ["SpaceInArrayBrackets"] = "spaceInArrayBrackets",
            ["NewLineAfterOpenBrace"] = "newLineAfterOpenBrace",
            ["NewLineAfterInclude"] = "newLineAfterInclude",
            ["PreserveEmptyLines"] = "preserveEmptyLines",
            ["MaxConsecutiveEmptyLines"] = "maxConsecutiveEmptyLines",
            ["SortIncludes"] = "sortIncludes",
            ["RequireSemicolons"] = "requireSemicolons",
            ["AllowSyntaxRecovery"] = "allowSyntaxRecovery",
            ["AllowUnsafeMacros"] = "allowUnsafeMacros",
            ["LineEnding"] = "lineEnding",
        };

        expectedIds.Keys.Should().BeEquivalentTo(FormattingOptionsCatalog.PropertyNames);

        foreach (var id in expectedIds.Values)
            html.Should().Contain($"id=\"{id}\"");
    }

    [Fact]
    public void Desktop_UI_wires_every_catalog_property()
    {
        var xaml = ReadRepoFile("src/SpFormatter.UI/MainWindow.xaml");
        var codeBehind = ReadRepoFile("src/SpFormatter.UI/MainWindow.xaml.cs");

        codeBehind.Should().NotContain("MaxLineLength");
        xaml.Should().NotContain("MaxLineLength");

        var expectedNames = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["IndentSize"] = ["IndentSizeTextBox"],
            ["UseSpaces"] = ["UseSpacesCheckBox"],
            ["SpaceAfterComma"] = ["SpaceAfterCommaCheckBox"],
            ["SpaceAroundOperators"] = ["SpaceAroundOperatorsCheckBox"],
            ["SpaceBeforeOpenParen"] = ["SpaceBeforeOpenParenCheckBox"],
            ["SpaceInArrayBrackets"] = ["SpaceInArrayBracketsCheckBox"],
            ["NewLineAfterOpenBrace"] = ["NewLineAfterOpenBraceCheckBox"],
            ["NewLineAfterInclude"] = ["NewLineAfterIncludeCheckBox"],
            ["PreserveEmptyLines"] = ["PreserveEmptyLinesCheckBox"],
            ["MaxConsecutiveEmptyLines"] = ["MaxConsecutiveEmptyLinesTextBox"],
            ["SortIncludes"] = ["SortIncludesCheckBox"],
            ["RequireSemicolons"] = ["RequireSemicolonsCheckBox"],
            ["AllowSyntaxRecovery"] = ["AllowSyntaxRecoveryCheckBox"],
            ["AllowUnsafeMacros"] = ["AllowUnsafeMacrosCheckBox"],
            ["LineEnding"] = ["LineEndingComboBox"],
        };

        expectedNames.Keys.Should().BeEquivalentTo(FormattingOptionsCatalog.PropertyNames);

        foreach (var (property, controlNames) in expectedNames)
        {
            foreach (var control in controlNames)
            {
                xaml.Should().Contain(control, because: $"{property} needs {control} in XAML");
                codeBehind.Should().Contain(control, because: $"{property} needs {control} wired in code-behind");
            }

            codeBehind.Should().Contain(property + " =", because: $"{property} must be assigned in GetFormattingOptionsFromUI");
        }
    }

    [Fact]
    public void Cli_help_documents_every_catalog_flag_token()
    {
        var programSource = ReadRepoFile("src/SpFormatter.Cli/Program.cs");
        foreach (var token in FormattingOptionsCatalog.CliFlagTokens)
        {
            programSource.Should().Contain($"\"{token}\"", because: $"CLI switch must handle {token}");
            Regex.IsMatch(programSource, Regex.Escape(token)).Should().BeTrue(
                because: $"CLI help/source must mention {token}");
        }
    }

    private static string ReadRepoFile(string relativePath)
    {
        var root = FindProjectRoot(Directory.GetCurrentDirectory());
        var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue($"missing repo file {relativePath}");
        return File.ReadAllText(fullPath);
    }

    private static string FindProjectRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory != null
               && !File.Exists(Path.Combine(directory.FullName, "SpFormatter.slnx"))
               && !File.Exists(Path.Combine(directory.FullName, "SpFormatter.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? startPath;
    }
}
