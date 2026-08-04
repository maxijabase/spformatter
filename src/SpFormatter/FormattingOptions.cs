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

    /// <summary>
    /// Space before <c>(</c> on calls and function/method names only.
    /// Control headers (<c>if</c>, <c>for</c>, <c>while</c>, <c>switch</c>, <c>do</c>/<c>while</c>)
    /// always use a space before <c>(</c>.
    /// </summary>
    public bool SpaceBeforeOpenParen { get; set; } = false;

    public bool SpaceInArrayBrackets { get; set; } = false;

    public bool NewLineAfterOpenBrace { get; set; } = true;
    public bool NewLineAfterInclude { get; set; } = true;

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

    /// <summary>
    /// When false (default), refuse to format files that contain function-like <c>#define Name(</c> macros.
    /// Those macros can inject syntax the AST does not see; formatting them can break compiling plugins.
    /// </summary>
    public bool AllowUnsafeMacros { get; set; } = false;

    public string LineEnding { get; set; } = Environment.NewLine;

    public static FormattingOptions Default => new();
}
