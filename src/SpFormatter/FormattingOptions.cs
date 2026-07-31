namespace SpFormatter;

/// <summary>
/// Options the formatter actually honors. Do not add knobs here until the printer implements them
/// and tests show true/false (or equivalent) outputs that differ.
/// </summary>
public class FormattingOptions
{
    public int IndentSize { get; set; } = 4;
    public bool UseSpaces { get; set; } = true;
    public string IndentString => UseSpaces ? new string(' ', IndentSize) : "\t";

    public bool SpaceAfterComma { get; set; } = true;
    public bool SpaceAroundOperators { get; set; } = true;
    public bool SpaceBeforeOpenParen { get; set; } = false;
    public bool SpaceInArrayBrackets { get; set; } = false;

    public bool NewLineAfterOpenBrace { get; set; } = true;
    public bool NewLineAfterInclude { get; set; } = true;

    public int MaxLineLength { get; set; } = 120;

    public bool PreserveEmptyLines { get; set; } = true;
    public int MaxConsecutiveEmptyLines { get; set; } = 2;

    /// <summary>
    /// Opt-in. Prefer preserving source order; sorting is deferred as a stable default.
    /// </summary>
    public bool SortIncludes { get; set; } = false;

    public bool RequireSemicolons { get; set; } = true;

    /// <summary>
    /// When false (default), syntax errors fail closed. When true, legacy ERROR-tree and expression-wrapper recovery may run.
    /// </summary>
    public bool AllowSyntaxRecovery { get; set; } = false;

    public string LineEnding { get; set; } = Environment.NewLine;

    public static FormattingOptions Default => new();
}
