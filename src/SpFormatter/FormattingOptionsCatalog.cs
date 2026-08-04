namespace SpFormatter;

/// <summary>
/// Honored formatting knobs and their CLI surface names.
/// Every entry must exist on <see cref="FormattingOptions"/> and be reachable from
/// CLI, playground, and desktop UI. Do not add engine options without updating this list
/// and all four surfaces in the same change.
/// </summary>
public static class FormattingOptionsCatalog
{
    public sealed record Entry(
        string PropertyName,
        string CliFlag,
        string DefaultSummary);

    public static IReadOnlyList<Entry> All { get; } =
    [
        new("IndentSize", "--indent", "4"),
        new("UseSpaces", "--use-tabs", "true (tabs via --use-tabs)"),
        new("SpaceAfterComma", "--no-space-after-comma", "true"),
        new("SpaceAroundOperators", "--no-space-around-operators", "true"),
        new("SpaceBeforeOpenParen", "--space-before-paren", "false"),
        new("SpaceInArrayBrackets", "--space-in-array-brackets", "false"),
        new("NewLineAfterOpenBrace", "--no-newline-after-open-brace", "true"),
        new("NewLineAfterInclude", "--no-newline-after-include", "true"),
        new("PreserveEmptyLines", "--no-preserve-empty-lines", "true"),
        new("MaxConsecutiveEmptyLines", "--max-consecutive-empty-lines", "2"),
        new("SortIncludes", "--sort-includes", "false"),
        new("RequireSemicolons", "--no-require-semicolons", "true"),
        new("AllowSyntaxRecovery", "--allow-syntax-recovery", "false"),
        new("AllowUnsafeMacros", "--unsafe-macros", "false"),
        new("LineEnding", "--lf / --crlf", "Environment.NewLine"),
    ];

    public static IReadOnlyList<string> PropertyNames { get; } =
        All.Select(e => e.PropertyName).ToArray();

    public static IReadOnlyList<string> CliFlagTokens { get; } =
        All.SelectMany(e => e.CliFlag.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(t => t.StartsWith("--", StringComparison.Ordinal)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
