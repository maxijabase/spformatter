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
            case "function_definition":
                if (IsMisparsedFunctionDefinition(node))
                {
                    result = string.Empty;
                    return false;
                }
                if (!TryFormatFunctionDefinition(node, indentLevel, out result))
                    return false;
                return true;
            case "function_declaration":
            case "native_declaration":
                result = FormatFunctionDeclaration(node, indentLevel);
                return true;
            case "parameter_declarations":
                result = FormatParameterDeclarations(node);
                return true;
            case "parameter_declaration":
                result = FormatParameterDeclaration(node);
                return true;
            case "type":
                result = FormatType(node);
                return true;
            case "array_access":
            case "array_indexed_access":
            case "fixed_dimension":
                result = FormatArrayAccess(node);
                return true;
            case "block":
                result = FormatBlock(node, indentLevel);
                return true;
            case "expression_statement":
                result = FormatExpressionStatement(node, indentLevel);
                return true;
            case "break_statement":
            case "continue_statement":
                result = FormatBreakContinueStatement(node, indentLevel);
                return true;
            case "return_statement":
                result = FormatReturnStatement(node, indentLevel);
                return true;
            default:
                result = string.Empty;
                return false;
        }
    }

    /// <summary>
    /// Single-line block used when NewLineAfterOpenBrace is false on function bodies.
    /// </summary>
    public string PrintCompactBlock(Node node) => FormatBlockCompact(node);

    private string FormatArrayAccess(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == "[")
            {
                parts.Add(_layout.Options.SpaceInArrayBrackets ? "[ " : "[");
                continue;
            }

            if (child.Type == "]")
            {
                parts.Add(_layout.Options.SpaceInArrayBrackets ? " ]" : "]");
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return string.Join("", parts);
    }

    private static bool IsMisparsedFunctionDefinition(Node node)
    {
        string? nameText = null;
        var hasParameters = false;
        var hasExpressionStatement = false;

        foreach (var child in node.Children)
        {
            if (child.Type == "identifier" && nameText == null)
                nameText = child.Text;
            else if (child.Type == "parameter_declarations")
                hasParameters = true;
            else if (child.Type == "expression_statement")
                hasExpressionStatement = true;
        }

        if (nameText is "if" or "else" or "for" or "while" or "switch" or "do")
            return true;

        return hasParameters && hasExpressionStatement;
    }

    private bool TryFormatFunctionDefinition(Node node, int indentLevel, out string result)
    {
        Node? visibility = null, returnType = null, functionName = null, parameters = null, body = null;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "visibility":
                    visibility = child;
                    break;
                case "type":
                    returnType = child;
                    break;
                case "identifier":
                    functionName ??= child;
                    break;
                case "parameter_declarations":
                    parameters = child;
                    break;
                case "block":
                    body = child;
                    break;
            }
        }

        var signature = _layout.Indent(indentLevel);
        if (visibility != null)
            signature += _formatChild(visibility, 0) + " ";
        if (returnType != null)
            signature += _formatChild(returnType, 0) + " ";
        if (functionName != null)
            signature += _formatChild(functionName, 0);
        if (parameters != null)
        {
            var paramsText = _formatChild(parameters, 0);
            if (_layout.Options.SpaceBeforeOpenParen && paramsText.StartsWith('('))
                signature += " ";
            signature += paramsText;
        }

        if (body == null)
        {
            result = signature;
            return true;
        }

        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            result = signature + " " + FormatBlockCompact(body);
            return true;
        }

        result = signature + _layout.Options.LineEnding + _formatChild(body, indentLevel);
        return true;
    }

    private string FormatBlock(Node node, int indentLevel)
    {
        var result = new List<string>
        {
            _layout.Indent(indentLevel) + "{"
        };

        foreach (var child in node.Children)
        {
            if (child.Type is "{" or "}")
                continue;

            var formatted = _formatChild(child, indentLevel + 1);
            if (string.IsNullOrWhiteSpace(formatted))
                continue;

            if (_layout.Options.RequireSemicolons
                && NeedsStatementSemicolon(child.Type)
                && !formatted.TrimEnd().EndsWith(";")
                && !formatted.Contains('{'))
            {
                formatted = formatted.TrimEnd() + ";";
            }
            else if (_layout.Options.RequireSemicolons
                     && LooksLikeStatementNeedingSemicolon(formatted)
                     && !formatted.TrimEnd().EndsWith(";")
                     && !formatted.Contains('{'))
            {
                formatted = formatted.TrimEnd() + ";";
            }

            result.Add(formatted);
        }

        result.Add(_layout.Indent(indentLevel) + "}");
        return string.Join(_layout.Options.LineEnding, result);
    }

    private string FormatBlockCompact(Node node)
    {
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            if (child.Type is "{" or "}")
                continue;

            var formatted = _formatChild(child, 0);
            if (string.IsNullOrWhiteSpace(formatted))
                continue;

            if (_layout.Options.RequireSemicolons
                && NeedsStatementSemicolon(child.Type)
                && !formatted.TrimEnd().EndsWith(";")
                && !formatted.Contains('{'))
            {
                formatted = formatted.TrimEnd() + ";";
            }
            else if (_layout.Options.RequireSemicolons
                     && LooksLikeStatementNeedingSemicolon(formatted)
                     && !formatted.TrimEnd().EndsWith(";")
                     && !formatted.Contains('{'))
            {
                formatted = formatted.TrimEnd() + ";";
            }

            parts.Add(formatted.Trim());
        }

        if (parts.Count == 0)
            return "{ }";

        return "{ " + string.Join(" ", parts) + " }";
    }

    private static bool NeedsStatementSemicolon(string nodeType) =>
        nodeType is "call_expression" or "assignment_expression" or "update_expression";

    private static bool LooksLikeStatementNeedingSemicolon(string formatted)
    {
        var trimmed = formatted.Trim();
        if (trimmed.Contains('(') && trimmed.EndsWith(')'))
            return true;
        if (trimmed.Contains('='))
            return true;
        if (trimmed.EndsWith("++") || trimmed.StartsWith("++")
            || trimmed.EndsWith("--") || trimmed.StartsWith("--"))
            return true;
        return false;
    }

    private string FormatExpressionStatement(Node node, int indentLevel)
    {
        var parts = new List<string>();
        var hasSemicolon = false;

        foreach (var child in node.Children)
        {
            if (child.Type == ";")
            {
                hasSemicolon = true;
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var joined = string.Join(" ", parts);
        if ((hasSemicolon || _layout.Options.RequireSemicolons) && !joined.EndsWith(";"))
            joined += ";";

        return _layout.Indent(indentLevel) + joined;
    }

    private string FormatBreakContinueStatement(Node node, int indentLevel)
    {
        var keyword = node.Type == "break_statement" ? "break" : "continue";
        var hasSemicolon = false;
        foreach (var child in node.Children)
        {
            if (child.Type == ";")
            {
                hasSemicolon = true;
                break;
            }
        }

        if (!hasSemicolon && node.Text.Contains(';'))
            hasSemicolon = true;

        var semi = (hasSemicolon || _layout.Options.RequireSemicolons) ? ";" : "";
        return _layout.Indent(indentLevel) + keyword + semi;
    }

    private string FormatReturnStatement(Node node, int indentLevel)
    {
        var parts = new List<string> { "return" };

        foreach (var child in node.Children)
        {
            if (child.Type is ";" or "return")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(" " + formatted);
        }

        return _layout.Indent(indentLevel) + string.Join("", parts) + ";";
    }

    private string FormatFunctionDeclaration(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == ";")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var signature = _layout.Indent(indentLevel);
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (i > 0)
            {
                if (part.StartsWith('('))
                    signature += _layout.Options.SpaceBeforeOpenParen ? " " : "";
                else
                    signature += " ";
            }

            signature += part;
        }

        return signature + ";";
    }

    private string FormatParameterDeclarations(Node node)
    {
        var parameters = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type is "(" or ")" or ",")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parameters.Add(formatted);
        }

        if (parameters.Count == 0)
            return "()";

        return "(" + _layout.JoinComma(parameters) + ")";
    }

    private string FormatParameterDeclaration(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = FormatDeclarationChild(child);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.JoinDeclarationParts(parts);
    }

    private string FormatType(Node node)
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
