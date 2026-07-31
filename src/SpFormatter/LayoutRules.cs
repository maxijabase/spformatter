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

    public bool IsAssignmentOperator(string nodeType) =>
        nodeType is "=" or "+=" or "-=" or "*=" or "/=" or "%="
            or "&=" or "|=" or "^=" or "<<=" or ">>=";

    public bool IsBinaryOrAssignmentOperator(string nodeType) =>
        IsAssignmentOperator(nodeType)
            || nodeType is "+" or "-" or "*" or "/" or "%"
            or "==" or "!=" or "<" or ">" or "<=" or ">="
            or "&&" or "||"
            or "&" or "|" or "^" or "<<" or ">>";

    /// <summary>
    /// Joins declaration tokens: spaces between words, comma policy, no space before '['.
    /// Operator tokens should already include spacing from <see cref="FormatAssignmentOperator"/>.
    /// </summary>
    public string JoinDeclarationParts(IReadOnlyList<string> parts)
    {
        if (parts.Count == 0)
            return string.Empty;

        var result = new System.Text.StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            var current = parts[i];
            if (i == 0)
            {
                result.Append(current);
                continue;
            }

            var previous = parts[i - 1];

            if (current == ",")
            {
                result.Append(",");
            }
            else if (previous == ",")
            {
                result.Append(_options.SpaceAfterComma ? " " + current : current);
            }
            else if (current.StartsWith('['))
            {
                result.Append(current);
            }
            else if (previous.EndsWith(':'))
            {
                // Old-type tags keep the colon glued: Handle:x, not Handle : x / Handle: x.
                result.Append(current);
            }
            else if (current.StartsWith('('))
            {
                if (_options.SpaceBeforeOpenParen)
                    result.Append(' ');
                result.Append(current);
            }
            else if (IsCompoundOperatorFragment(previous, current))
            {
                result.Append(current);
            }
            else if (StartsWithOperatorSpacing(current) || EndsWithOperatorSpacing(previous))
            {
                result.Append(current);
            }
            else
            {
                result.Append(' ');
                result.Append(current);
            }
        }

        return result.ToString();
    }

    private static bool StartsWithOperatorSpacing(string text) =>
        text.StartsWith(' ') || text.StartsWith('=') || text.StartsWith('+') || text.StartsWith('-')
        || text.StartsWith('*') || text.StartsWith('/') || text.StartsWith('%')
        || text.StartsWith('<') || text.StartsWith('>') || text.StartsWith('!')
        || text.StartsWith('&') || text.StartsWith('|') || text.StartsWith('^');

    private static bool EndsWithOperatorSpacing(string text) =>
        text.EndsWith(' ') || text.EndsWith('=') || text.EndsWith('+') || text.EndsWith('-')
        || text.EndsWith('*') || text.EndsWith('/') || text.EndsWith('%')
        || text.EndsWith('<') || text.EndsWith('>') || text.EndsWith('!')
        || text.EndsWith('&') || text.EndsWith('|') || text.EndsWith('^');

    private static bool IsCompoundOperatorFragment(string previous, string current) =>
        (previous == "=" && current == "=") ||
        (previous == "!" && current == "=") ||
        (previous == "<" && current == "=") ||
        (previous == ">" && current == "=") ||
        (previous == "+" && current == "=") ||
        (previous == "-" && current == "=") ||
        (previous == "*" && current == "=") ||
        (previous == "/" && current == "=") ||
        (previous == "%" && current == "=") ||
        (previous == "&" && current == "&") ||
        (previous == "|" && current == "|") ||
        (previous == "&" && current == "=") ||
        (previous == "|" && current == "=") ||
        (previous == "^" && current == "=") ||
        (previous == "<" && current == "<") ||
        (previous == ">" && current == ">") ||
        (previous == "+" && current == "+") ||
        (previous == "-" && current == "-");
}
