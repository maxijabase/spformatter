using TreeSitter;

namespace SpFormatter;

/// <summary>
/// Typed pretty-printer for constructs that have been migrated off the legacy god class.
/// Returns false from TryPrint when the node type is still owned by the legacy path.
/// </summary>
public sealed class AstPrinter
{
    private readonly LayoutRules _layout;
    private readonly Func<Node, int, string> _formatChild;

    public AstPrinter(LayoutRules layout, Func<Node, int, string> formatChild)
    {
        _layout = layout;
        _formatChild = formatChild;
    }

    public bool TryPrint(Node node, int indentLevel, out string result)
    {
        switch (node.Type)
        {
            case "binary_expression":
                result = FormatBinaryExpression(node);
                return true;
            case "unary_expression":
            case "update_expression":
                result = FormatUnaryOrUpdateExpression(node);
                return true;
            case "assignment_expression":
                result = FormatAssignmentExpression(node, indentLevel, asStatement: false);
                return true;
            case "assignment_statement":
                result = FormatAssignmentExpression(node, indentLevel, asStatement: true);
                return true;
            case "call_expression":
                result = FormatCallExpression(node);
                return true;
            case "call_arguments":
                result = FormatCallArguments(node);
                return true;
            case "string_literal":
            case "character_literal":
            case "number_literal":
            case "identifier":
            case "builtin_type":
            case "visibility":
                result = node.Text;
                return true;
            case "ternary_expression":
            case "conditional_expression":
                result = FormatTernaryExpression(node);
                return true;
            case "variable_declaration":
            case "declaration_statement":
            case "global_variable_declaration":
            case "variable_declaration_statement":
            case "old_global_variable_declaration":
            case "old_variable_declaration":
                result = FormatVariableDeclaration(node, indentLevel);
                return true;
            default:
                result = string.Empty;
                return false;
        }
    }

    private string FormatVariableDeclaration(Node node, int indentLevel)
    {
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            if (child.Type == ";")
                continue;

            if (child.Type is "variable_declaration" or "old_variable_declaration")
            {
                var nested = FormatVariableDeclarationInner(child);
                if (!string.IsNullOrEmpty(nested))
                    parts.Add(nested);
                continue;
            }

            var formatted = FormatDeclarationChild(child);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var joined = _layout.JoinDeclarationParts(parts);
        if (_layout.Options.RequireSemicolons && !joined.EndsWith(';'))
            joined += ";";

        return _layout.Indent(indentLevel) + joined;
    }

    private string FormatVariableDeclarationInner(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == ";")
                continue;

            var formatted = FormatDeclarationChild(child);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.JoinDeclarationParts(parts);
    }

    private string FormatDeclarationChild(Node child)
    {
        var formatted = _formatChild(child, 0);
        if (string.IsNullOrEmpty(formatted))
            return string.Empty;

        if (_layout.IsBinaryOrAssignmentOperator(child.Type))
            return _layout.FormatAssignmentOperator(formatted.Trim());

        return formatted;
    }

    private string FormatBinaryExpression(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (string.IsNullOrEmpty(formatted))
                continue;

            if (_layout.IsBinaryOrAssignmentOperator(child.Type))
                parts.Add(_layout.FormatBinaryOperator(formatted.Trim()));
            else
                parts.Add(formatted);
        }

        return string.Join("", parts);
    }

    private string FormatUnaryOrUpdateExpression(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return string.Join("", parts);
    }

    private string FormatAssignmentExpression(Node node, int indentLevel, bool asStatement)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (string.IsNullOrEmpty(formatted))
                continue;

            if (child.Type is "=" or "+=" or "-=" or "*=" or "/=" or "%=")
                parts.Add(_layout.FormatAssignmentOperator(formatted.Trim()));
            else
                parts.Add(formatted);
        }

        var body = string.Join("", parts);
        if (asStatement)
        {
            var indented = _layout.Indent(indentLevel) + body;
            if (_layout.Options.RequireSemicolons && !indented.TrimEnd().EndsWith(";"))
                indented += ";";
            return indented;
        }

        return body;
    }

    private string FormatCallExpression(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        if (parts.Count >= 2 && parts[1].StartsWith("("))
        {
            if (_layout.Options.SpaceBeforeOpenParen)
                return parts[0] + " " + string.Join("", parts.Skip(1));
            return string.Join("", parts);
        }

        return string.Join("", parts);
    }

    private string FormatCallArguments(Node node)
    {
        var arguments = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type is "(" or ")" or ",")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                arguments.Add(formatted);
        }

        if (arguments.Count == 0)
            return "()";

        return "(" + _layout.JoinComma(arguments) + ")";
    }

    private string FormatTernaryExpression(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (string.IsNullOrEmpty(formatted))
                continue;

            if (child.Type == "?")
                parts.Add(_layout.Options.SpaceAroundOperators ? " ? " : "?");
            else if (child.Type == ":")
                parts.Add(_layout.Options.SpaceAroundOperators ? " : " : ":");
            else
                parts.Add(formatted);
        }

        return string.Join("", parts);
    }
}
