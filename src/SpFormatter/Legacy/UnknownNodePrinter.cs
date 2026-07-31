using System.Text;
using System.Text.RegularExpressions;
using TreeSitter;

namespace SpFormatter.Legacy;

/// <summary>
/// Fallback printer for node types not yet (or never) owned by AstPrinter.
/// Used mainly under AllowSyntaxRecovery for ERROR trees.
/// </summary>
public sealed class UnknownNodePrinter
{
    private readonly LayoutRules _layout;
    private readonly Func<Node, int, string> _formatChild;

    public UnknownNodePrinter(LayoutRules layout, Func<Node, int, string> formatChild)
    {
        _layout = layout;
        _formatChild = formatChild;
    }

    public string Print(Node node, int indentLevel)
    {
        if (node.Children.Count > 0 && node.IsNamed)
        {
            var parts = new List<string>();
            foreach (var child in node.Children)
            {
                var formatted = _formatChild(child, indentLevel);
                if (!string.IsNullOrEmpty(formatted))
                    parts.Add(formatted);
            }

            var result = new StringBuilder();
            for (var i = 0; i < parts.Count; i++)
            {
                if (i == 0)
                {
                    result.Append(parts[i]);
                    continue;
                }

                var current = parts[i];
                var previous = parts[i - 1];

                if ((previous.EndsWith("++") || previous.EndsWith("--") || previous.EndsWith("!")) &&
                    (Regex.IsMatch(current, @"^\w") || current.StartsWith('i')))
                {
                    result.Append(current);
                }
                else if (current is "[" or "]" || previous is "[" or "]" ||
                         current.StartsWith('[') || current.EndsWith(']'))
                {
                    result.Append(current);
                }
                else if (current == "(" || current.StartsWith('(') || previous == ")")
                {
                    result.Append(current);
                }
                else if (current == ")" || previous == "(" || previous.StartsWith('('))
                {
                    result.Append(current);
                }
                else if (current is "<" or ">" || previous is "<" or ">")
                {
                    result.Append(current);
                }
                else if (current == "." || previous == ".")
                {
                    result.Append(current);
                }
                else if (current == ";")
                {
                    result.Append(current);
                }
                else if (current is "?" or ":")
                {
                    result.Append(" " + current + " ");
                }
                else if (previous is "?" or ":")
                {
                    result.Append(current);
                }
                else if ((previous == "=" && current == "=") ||
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
                         (previous == "-" && current == "-"))
                {
                    result.Append(current);
                }
                else
                {
                    result.Append(" " + current);
                }
            }

            return result.ToString();
        }

        if (indentLevel > 0 && !node.Text.Contains('\n'))
            return _layout.Indent(indentLevel) + node.Text.Trim();

        return node.Text;
    }
}
