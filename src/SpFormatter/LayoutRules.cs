namespace SpFormatter;

/// <summary>
/// Single place for spacing, indent, and join policy driven by <see cref="FormattingOptions"/>.
/// Construct printers should ask this type instead of inventing their own spacing.
/// </summary>
public sealed class LayoutRules
{
    private readonly FormattingOptions _options;

    public LayoutRules(FormattingOptions options)
    {
        _options = options;
    }

    public FormattingOptions Options => _options;

    public string Indent(int level) =>
        string.Concat(Enumerable.Repeat(_options.IndentString, level));

    public string JoinComma(IEnumerable<string> parts) =>
        string.Join(_options.SpaceAfterComma ? ", " : ",", parts);

    public string FormatBinaryOperator(string op) =>
        _options.SpaceAroundOperators ? $" {op} " : op;

    public string FormatAssignmentOperator(string op) =>
        _options.SpaceAroundOperators ? $" {op} " : op;

    public string CallWithParen(string callee, string argsInsideParens)
    {
        var space = _options.SpaceBeforeOpenParen ? " " : "";
        return $"{callee}{space}({argsInsideParens})";
    }

    public string ArrayAccess(string target, string index)
    {
        if (_options.SpaceInArrayBrackets)
            return $"{target}[ {index} ]";
        return $"{target}[{index}]";
    }

    public bool IsBinaryOrAssignmentOperator(string nodeType) =>
        nodeType is "=" or "+=" or "-=" or "*=" or "/=" or "%="
            or "+" or "-" or "*" or "/" or "%"
            or "==" or "!=" or "<" or ">" or "<=" or ">="
            or "&&" or "||"
            or "&" or "|" or "^" or "<<" or ">>";
}
