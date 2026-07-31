using System.Linq;
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
            case "int_literal":
            case "float_literal":
            case "bool_literal":
            case "identifier":
            case "builtin_type":
            case "visibility":
                result = node.Text;
                return true;
            case "(":
            case ")":
            case "{":
            case "}":
            case ";":
            case ",":
            case ":":
            case "?":
            case ".":
            case "[":
            case "]":
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
            case "old_variable_declaration_statement":
                result = FormatVariableDeclaration(node, indentLevel);
                return true;
            case "old_type_cast":
                result = FormatOldTypeCast(node);
                return true;
            case "function_definition":
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
            case "condition_statement":
                result = FormatConditionStatement(node, indentLevel);
                return true;
            case "for_statement":
                result = FormatForStatement(node, indentLevel);
                return true;
            case "while_statement":
                result = FormatWhileStatement(node, indentLevel);
                return true;
            case "switch_statement":
                result = FormatSwitchStatement(node, indentLevel);
                return true;
            case "switch_case":
                result = FormatSwitchCase(node, indentLevel);
                return true;
            case "comment":
            case "line_comment":
            case "block_comment":
                result = FormatComment(node, indentLevel);
                return true;
            case "preproc_include":
            case "preproc_define":
            case "preproc_pragma":
            case "preproc_if":
            case "preproc_ifdef":
            case "preproc_ifndef":
            case "preproc_else":
            case "preproc_endif":
            case "preproc_endinput":
            case "preproc_undef":
                result = FormatPreprocessor(node);
                return true;
            case "source_file":
                if (node.HasError)
                {
                    result = string.Empty;
                    return false;
                }
                result = FormatSourceFile(node, indentLevel);
                return true;
            case "methodmap":
                result = FormatMethodmap(node, indentLevel);
                return true;
            case "methodmap_alias":
            case "methodmap_native":
            case "methodmap_native_constructor":
            case "methodmap_native_destructor":
                result = FormatMethodmapMemberDeclaration(node, indentLevel);
                return true;
            case "methodmap_method":
            case "methodmap_method_constructor":
            case "methodmap_method_destructor":
                result = FormatMethodmapMethod(node, indentLevel);
                return true;
            case "methodmap_property":
                result = FormatMethodmapProperty(node, indentLevel);
                return true;
            case "methodmap_property_alias":
            case "methodmap_property_native":
            case "methodmap_property_method":
                result = FormatMethodmapPropertyAccessor(node, indentLevel);
                return true;
            case "methodmap_property_getter":
            case "methodmap_property_setter":
                result = FormatMethodmapPropertyAccessorSig(node);
                return true;
            case "methodmap_visibility":
                result = "public";
                return true;
            case "enum":
                result = FormatEnum(node, indentLevel);
                return true;
            case "enum_entries":
                result = FormatEnumEntries(node, indentLevel);
                return true;
            case "enum_entry":
                result = FormatEnumEntry(node, indentLevel);
                return true;
            case "enum_struct":
                result = FormatEnumStruct(node, indentLevel);
                return true;
            case "enum_struct_field":
                result = FormatEnumStructField(node, indentLevel);
                return true;
            case "enum_struct_method":
                result = FormatEnumStructMethod(node, indentLevel);
                return true;
            case "alias_declaration":
                result = FormatAliasDeclaration(node, indentLevel);
                return true;
            case "alias_assignment":
                result = FormatAliasAssignment(node, indentLevel);
                return true;
            case "alias_operator":
            case "operator":
            case "function_declaration_kind":
                result = node.Text;
                return true;
            case "typedef":
                result = FormatTypedef(node, indentLevel);
                return true;
            case "typedef_expression":
                result = FormatTypedefExpression(node, indentLevel);
                return true;
            case "typeset":
                result = FormatTypeset(node, indentLevel);
                return true;
            case "functag":
                result = FormatFunctag(node, indentLevel);
                return true;
            case "funcenum":
                result = FormatFuncenum(node, indentLevel);
                return true;
            case "funcenum_member":
                result = FormatFuncenumMember(node, indentLevel);
                return true;
            case "struct":
                result = FormatStruct(node, indentLevel);
                return true;
            case "struct_field":
            case "old_struct_field":
                result = FormatStructField(node, indentLevel);
                return true;
            case "struct_declaration":
                result = FormatStructDeclaration(node, indentLevel);
                return true;
            case "struct_constructor":
                result = FormatStructConstructor(node, indentLevel);
                return true;
            case "struct_field_value":
                result = FormatStructFieldValue(node, indentLevel);
                return true;
            case "old_type":
                // Keep Tag: glued; never emit "Tag :" / "Tag: ".
                result = node.Text;
                return true;
            case "new":
                result = node.Text;
                return true;
            case "dimension":
                result = _layout.Options.SpaceInArrayBrackets ? "[ ]" : "[]";
                return true;
            case "array_type":
                result = FormatArrayType(node);
                return true;
            case "any_type":
            case "variable_storage_class":
            case "old_builtin_type":
            case "function":
            case "public":
            case "const":
            case "static":
            case "native":
            case "property":
            case "__nullable__":
            case "~":
            case "=":
            case "<":
            case "<<=":
            case ">>=":
            case "+=":
            case "-=":
            case "*=":
            case "/=":
            case "|=":
            case "&=":
            case "^=":
            case "~=":
                result = node.Text;
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

    private string FormatConditionStatement(Node node, int indentLevel)
    {
        Node? condition = null;
        Node? truePath = null;
        Node? falsePath = null;
        var inParens = false;
        var seenElse = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "if":
                    continue;
                case "(":
                    inParens = true;
                    continue;
                case ")":
                    inParens = false;
                    continue;
                case "else":
                    seenElse = true;
                    continue;
            }

            if (inParens)
            {
                condition ??= child;
                continue;
            }

            if (!seenElse)
                truePath ??= child;
            else
                falsePath ??= child;
        }

        var indent = _layout.Indent(indentLevel);
        var space = _layout.Options.SpaceBeforeOpenParen ? " " : "";
        var conditionText = condition != null ? _formatChild(condition, 0) : "";
        var lines = new List<string>
        {
            indent + "if" + space + "(" + conditionText + ")"
        };

        AppendControlBody(lines, truePath, indentLevel);

        if (falsePath != null)
        {
            if (falsePath.Type == "condition_statement")
            {
                var elseIfFormatted = FormatConditionStatement(falsePath, indentLevel);
                lines.Add(indent + "else " + elseIfFormatted[indent.Length..]);
            }
            else
            {
                lines.Add(indent + "else");
                AppendControlBody(lines, falsePath, indentLevel);
            }
        }

        return string.Join(_layout.Options.LineEnding, lines);
    }

    private void AppendControlBody(List<string> lines, Node? body, int indentLevel)
    {
        if (body == null)
            return;

        if (body.Type == "block")
        {
            if (_layout.Options.NewLineAfterOpenBrace)
                lines.Add(_formatChild(body, indentLevel));
            else
                lines[^1] += " " + _formatChild(body, indentLevel).Trim();
            return;
        }

        lines.Add(_formatChild(body, indentLevel + 1));
    }

    private string FormatForStatement(Node node, int indentLevel)
    {
        Node? initialization = null;
        Node? condition = null;
        Node? increment = null;
        Node? body = null;
        var inParens = false;
        var slot = 0;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "for":
                    continue;
                case "(":
                    inParens = true;
                    slot = 0;
                    continue;
                case ")":
                    inParens = false;
                    continue;
                case ";":
                    if (inParens)
                        slot++;
                    continue;
            }

            if (inParens)
            {
                switch (slot)
                {
                    case 0:
                        initialization ??= child;
                        break;
                    case 1:
                        condition ??= child;
                        break;
                    case 2:
                        increment ??= child;
                        break;
                }
                continue;
            }

            body ??= child;
        }

        var indent = _layout.Indent(indentLevel);
        var space = _layout.Options.SpaceBeforeOpenParen ? " " : "";
        var initText = initialization != null ? _formatChild(initialization, 0).TrimEnd(';') : "";
        var condText = condition != null ? _formatChild(condition, 0) : "";
        var incrText = increment != null ? _formatChild(increment, 0) : "";

        var lines = new List<string>
        {
            indent + "for" + space + "("
                + initText
                + ";"
                + (condText.Length > 0 ? " " + condText : "")
                + ";"
                + (incrText.Length > 0 ? " " + incrText : "")
                + ")"
        };
        AppendControlBody(lines, body, indentLevel);
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatWhileStatement(Node node, int indentLevel)
    {
        Node? condition = null;
        Node? body = null;
        var inParens = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "while":
                    continue;
                case "(":
                    inParens = true;
                    continue;
                case ")":
                    inParens = false;
                    continue;
            }

            if (inParens)
            {
                condition ??= child;
                continue;
            }

            body ??= child;
        }

        var indent = _layout.Indent(indentLevel);
        var space = _layout.Options.SpaceBeforeOpenParen ? " " : "";
        var conditionText = condition != null ? _formatChild(condition, 0) : "";
        var lines = new List<string>
        {
            indent + "while" + space + "(" + conditionText + ")"
        };
        AppendControlBody(lines, body, indentLevel);
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatSwitchStatement(Node node, int indentLevel)
    {
        Node? switchExpression = null;
        var cases = new List<Node>();
        var inParens = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "switch":
                case "{":
                case "}":
                    continue;
                case "(":
                    inParens = true;
                    continue;
                case ")":
                    inParens = false;
                    continue;
                case "switch_case":
                    cases.Add(child);
                    continue;
            }

            if (inParens)
                switchExpression ??= child;
        }

        var indent = _layout.Indent(indentLevel);
        var space = _layout.Options.SpaceBeforeOpenParen ? " " : "";
        var exprText = switchExpression != null ? _formatChild(switchExpression, 0) : "";
        var lines = new List<string>
        {
            indent + "switch" + space + "(" + exprText + ")",
            indent + "{"
        };

        foreach (var caseNode in cases)
            lines.Add(_formatChild(caseNode, indentLevel + 1));

        lines.Add(indent + "}");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatSwitchCase(Node node, int indentLevel)
    {
        var caseLineParts = new List<string>();
        Node? caseBody = null;
        var foundColon = false;

        foreach (var child in node.Children)
        {
            if (child.Type == "block")
            {
                caseBody = child;
                continue;
            }

            if (foundColon)
                continue;

            var formatted = child.Type is "case" or "default" or ":" or ","
                ? child.Text
                : _formatChild(child, 0);

            if (string.IsNullOrEmpty(formatted))
                continue;

            if (formatted == ":")
                foundColon = true;
            caseLineParts.Add(formatted);
        }

        var caseLineText = new System.Text.StringBuilder();
        for (var i = 0; i < caseLineParts.Count; i++)
        {
            var part = caseLineParts[i];
            if (i == 0)
            {
                caseLineText.Append(part);
                continue;
            }

            if (part == ":" || part == ",")
            {
                caseLineText.Append(part);
                continue;
            }

            if (caseLineParts[i - 1] == "case")
            {
                caseLineText.Append(' ').Append(part);
                continue;
            }

            if (caseLineParts[i - 1] == ",")
            {
                caseLineText.Append(_layout.Options.SpaceAfterComma ? " " + part : part);
                continue;
            }

            caseLineText.Append(' ').Append(part);
        }

        var lines = new List<string>
        {
            _layout.Indent(indentLevel) + caseLineText
        };

        if (caseBody != null)
            lines.Add(_formatChild(caseBody, indentLevel));

        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatComment(Node node, int indentLevel)
    {
        var indent = _layout.Indent(indentLevel);
        var commentText = node.Text;

        if (commentText.StartsWith("//", StringComparison.Ordinal))
            return indent + commentText.Replace("\r\n", "\n").Replace('\r', '\n')
                .Replace("\n", _layout.Options.LineEnding);

        if (commentText.StartsWith("/*", StringComparison.Ordinal))
        {
            var lines = commentText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            var result = new List<string> { indent + lines[0] };

            for (var i = 1; i < lines.Length; i++)
            {
                if (i == lines.Length - 1 && lines[i].Trim() == "*/")
                    result.Add(indent + " */");
                else
                    result.Add(indent + " " + lines[i].TrimStart());
            }

            return string.Join(_layout.Options.LineEnding, result);
        }

        return indent + commentText;
    }

    private string FormatPreprocessor(Node node) => node.Text;

    private string FormatMethodmap(Node node, int indentLevel)
    {
        var header = BuildMethodmapHeader(node, indentLevel);
        var members = new List<string>();
        var inBody = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "{":
                    inBody = true;
                    break;
                case "}":
                case ";":
                case "methodmap":
                case "identifier":
                case "<":
                case "__nullable__":
                    break;
                default:
                    if (!inBody)
                        break;
                    var member = _formatChild(child, indentLevel + 1);
                    if (!string.IsNullOrWhiteSpace(member))
                        members.Add(member);
                    break;
            }
        }

        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", members.Select(m => m.Trim()));
            return header + " { " + inner + " };";
        }

        var lines = new List<string> { header, _layout.Indent(indentLevel) + "{" };
        lines.AddRange(members);
        lines.Add(_layout.Indent(indentLevel) + "};");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string BuildMethodmapHeader(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "methodmap":
                    parts.Add("methodmap");
                    break;
                case "identifier":
                    parts.Add(_formatChild(child, 0));
                    break;
                case "<":
                    parts.Add("<");
                    break;
                case "__nullable__":
                    parts.Add("__nullable__");
                    break;
                case "{":
                    goto done;
            }
        }

    done:
        var header = _layout.Indent(indentLevel);
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (i > 0)
            {
                if (part == "<")
                    header += " < ";
                else if (parts[i - 1] == "<")
                    header += part;
                else
                    header += " " + part;
            }
            else
            {
                header += part;
            }
        }

        return header;
    }

    private string FormatMethodmapMemberDeclaration(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == ";")
                continue;

            if (child.Type == "=")
            {
                parts.Add("=");
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.Indent(indentLevel) + JoinMethodmapSignatureParts(parts) + ";";
    }

    private string FormatMethodmapMethod(Node node, int indentLevel)
    {
        Node? body = null;
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == "block")
            {
                body = child;
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var signature = _layout.Indent(indentLevel) + JoinMethodmapSignatureParts(parts);
        if (body == null)
            return signature;

        if (!_layout.Options.NewLineAfterOpenBrace)
            return signature + " " + FormatBlockCompact(body);

        return signature + _layout.Options.LineEnding + _formatChild(body, indentLevel);
    }

    private string FormatMethodmapProperty(Node node, int indentLevel)
    {
        var headerParts = new List<string>();
        var accessors = new List<string>();
        var inBody = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "{":
                    inBody = true;
                    break;
                case "}":
                case ";":
                    break;
                default:
                    if (!inBody)
                    {
                        var part = _formatChild(child, 0);
                        if (!string.IsNullOrEmpty(part))
                            headerParts.Add(part);
                    }
                    else
                    {
                        var accessor = _formatChild(child, indentLevel + 1);
                        if (!string.IsNullOrWhiteSpace(accessor))
                            accessors.Add(accessor);
                    }

                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + string.Join(" ", headerParts);
        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", accessors.Select(a => a.Trim()));
            return header + " { " + inner + " }";
        }

        var lines = new List<string> { header, _layout.Indent(indentLevel) + "{" };
        lines.AddRange(accessors);
        lines.Add(_layout.Indent(indentLevel) + "}");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatMethodmapPropertyAccessor(Node node, int indentLevel)
    {
        Node? body = null;
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            if (child.Type == "block")
            {
                body = child;
                continue;
            }

            if (child.Type == ";")
                continue;

            if (child.Type == "=")
            {
                parts.Add("=");
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var signature = _layout.Indent(indentLevel) + JoinMethodmapSignatureParts(parts);
        if (body != null)
        {
            if (!_layout.Options.NewLineAfterOpenBrace)
                return signature + " " + FormatBlockCompact(body);
            return signature + _layout.Options.LineEnding + _formatChild(body, indentLevel);
        }

        return signature + ";";
    }

    private string FormatMethodmapPropertyAccessorSig(Node node)
    {
        // Grammar shapes: get() | set(parameter_declaration)
        if (node.Type == "methodmap_property_getter")
            return "get()";

        var param = node.Children.FirstOrDefault(c => c.Type == "parameter_declaration");
        if (param == null)
            return "set()";

        return "set(" + _formatChild(param, 0) + ")";
    }

    private static string JoinMethodmapSignatureParts(IReadOnlyList<string> parts)
    {
        var signature = "";
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (i == 0)
            {
                signature = part;
                continue;
            }

            if (part == "=")
            {
                signature += " = ";
                continue;
            }

            if (part.StartsWith('(') || part == ")" || part == "~")
            {
                if (part == "~")
                    signature += signature.Length > 0 && !signature.EndsWith(' ') ? " " + part : part;
                else
                    signature += part;
                continue;
            }

            if (signature.EndsWith("~") || signature.EndsWith("= ") || signature.EndsWith(':'))
            {
                signature += part;
                continue;
            }

            signature += " " + part;
        }

        return signature;
    }

    private string FormatEnum(Node node, int indentLevel)
    {
        var headerParts = new List<string> { "enum" };
        string? entries = null;
        var inIncrement = false;
        var incrementParts = new List<string>();

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "enum":
                case ";":
                    break;
                case "enum_entries":
                    entries = FormatEnumEntries(child, indentLevel);
                    break;
                case "(":
                    inIncrement = true;
                    break;
                case ")":
                    inIncrement = false;
                    break;
                case ":":
                    if (headerParts.Count > 0)
                        headerParts[^1] = headerParts[^1] + ":";
                    break;
                default:
                    if (inIncrement)
                    {
                        var part = _formatChild(child, 0);
                        if (string.IsNullOrEmpty(part))
                            break;
                        if (_layout.IsBinaryOrAssignmentOperator(child.Type))
                            incrementParts.Add(_layout.FormatAssignmentOperator(part.Trim()));
                        else
                            incrementParts.Add(part);
                    }
                    else
                    {
                        var part = _formatChild(child, 0);
                        if (!string.IsNullOrEmpty(part))
                            headerParts.Add(part);
                    }

                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + _layout.JoinDeclarationParts(headerParts);
        if (incrementParts.Count > 0)
            header += "(" + string.Join("", incrementParts).Trim() + ")";

        if (entries == null)
            return header + ";";

        if (!_layout.Options.NewLineAfterOpenBrace)
            return header + " " + entries.TrimStart() + ";";

        return header + _layout.Options.LineEnding + entries + ";";
    }

    private string FormatEnumEntries(Node node, int indentLevel)
    {
        var entries = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type is "{" or "}" or ",")
                continue;

            if (child.Type == "enum_entry")
            {
                entries.Add(FormatEnumEntry(child, indentLevel + 1) + ",");
                continue;
            }

            var formatted = _formatChild(child, indentLevel + 1);
            if (!string.IsNullOrWhiteSpace(formatted))
                entries.Add(formatted);
        }

        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", entries.Select(e => e.Trim()));
            return "{ " + inner + " }";
        }

        var lines = new List<string> { _layout.Indent(indentLevel) + "{" };
        lines.AddRange(entries);
        lines.Add(_layout.Indent(indentLevel) + "}");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatEnumEntry(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == ":")
            {
                if (parts.Count > 0)
                    parts[^1] = parts[^1] + ":";
                continue;
            }

            if (child.Type == "=")
            {
                parts.Add("=");
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.Indent(indentLevel) + JoinMethodmapSignatureParts(parts);
    }

    private string FormatAliasDeclaration(Node node, int indentLevel)
    {
        Node? body = null;
        var parts = new List<string>();

        foreach (var child in node.Children)
        {
            if (child.Type is "block" || child.Type.EndsWith("_statement"))
            {
                body = child;
                continue;
            }

            if (child.Type == ";")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var signature = _layout.Indent(indentLevel) + JoinAliasSignatureParts(parts);
        if (body == null)
            return signature + ";";

        if (!_layout.Options.NewLineAfterOpenBrace)
            return signature + " " + (body.Type == "block" ? FormatBlockCompact(body) : _formatChild(body, 0).Trim());

        return signature + _layout.Options.LineEnding + _formatChild(body, indentLevel);
    }

    private string FormatAliasAssignment(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == ";")
                continue;

            if (child.Type == "=")
            {
                parts.Add("=");
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.Indent(indentLevel) + JoinAliasSignatureParts(parts) + ";";
    }

    private static string JoinAliasSignatureParts(IReadOnlyList<string> parts)
    {
        // "operator" must stay glued to the alias operator: operator++ / operator*
        var signature = "";
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (i == 0)
            {
                signature = part;
                continue;
            }

            if (part == "=")
            {
                signature += " = ";
                continue;
            }

            if (part.StartsWith('(') || part == ")")
            {
                signature += part;
                continue;
            }

            if (signature.EndsWith("operator") || signature.EndsWith(':') || signature.EndsWith("= "))
            {
                signature += part;
                continue;
            }

            signature += " " + part;
        }

        return signature;
    }

    private string FormatEnumStruct(Node node, int indentLevel)
    {
        var name = "";
        var members = new List<string>();
        var inBody = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "enum":
                case "struct":
                    break;
                case "identifier":
                    if (!inBody)
                        name = _formatChild(child, 0);
                    break;
                case "{":
                    inBody = true;
                    break;
                case "}":
                    break;
                default:
                    if (!inBody)
                        break;
                    var member = _formatChild(child, indentLevel + 1);
                    if (!string.IsNullOrWhiteSpace(member))
                        members.Add(member);
                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + "enum struct " + name;
        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", members.Select(m => m.Trim()));
            return header + " { " + inner + " }";
        }

        var lines = new List<string> { header, _layout.Indent(indentLevel) + "{" };
        lines.AddRange(members);
        lines.Add(_layout.Indent(indentLevel) + "}");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatEnumStructField(Node node, int indentLevel)
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

        return _layout.Indent(indentLevel) + _layout.JoinDeclarationParts(parts) + ";";
    }

    private string FormatEnumStructMethod(Node node, int indentLevel)
    {
        // Same shape as a function definition without visibility.
        Node? body = null;
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type == "block")
            {
                body = child;
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var signature = _layout.Indent(indentLevel) + JoinMethodmapSignatureParts(parts);
        if (body == null)
            return signature;

        if (!_layout.Options.NewLineAfterOpenBrace)
            return signature + " " + FormatBlockCompact(body);

        return signature + _layout.Options.LineEnding + _formatChild(body, indentLevel);
    }

    private string FormatTypedef(Node node, int indentLevel)
    {
        var name = "";
        string? expression = null;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "typedef":
                case "=":
                case ";":
                    break;
                case "identifier":
                    name = _formatChild(child, 0);
                    break;
                case "typedef_expression":
                    expression = FormatTypedefExpression(child, 0);
                    break;
            }
        }

        return _layout.Indent(indentLevel) + "typedef " + name + " = " + (expression ?? "") + ";";
    }

    private string FormatTypedefExpression(Node node, int indentLevel)
    {
        var typeAndDims = new List<string>();
        string? parameters = null;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "(":
                case ")":
                case "function":
                    break;
                case "type":
                case "dimension":
                case "fixed_dimension":
                    var part = _formatChild(child, 0);
                    if (!string.IsNullOrEmpty(part))
                        typeAndDims.Add(part);
                    break;
                case "parameter_declarations":
                    parameters = _formatChild(child, 0);
                    break;
            }
        }

        var returnType = _layout.JoinDeclarationParts(typeAndDims);
        var signature = "function " + returnType;
        if (parameters != null)
        {
            if (_layout.Options.SpaceBeforeOpenParen)
                signature += " ";
            signature += parameters;
        }

        return _layout.Indent(indentLevel) + signature;
    }

    private string FormatTypeset(Node node, int indentLevel)
    {
        var name = "";
        var members = new List<string>();
        var inBody = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "typeset":
                case ";":
                    break;
                case "identifier":
                    if (!inBody)
                        name = _formatChild(child, 0);
                    break;
                case "{":
                    inBody = true;
                    break;
                case "}":
                    break;
                case "typedef_expression":
                    members.Add(FormatTypedefExpression(child, indentLevel + 1) + ";");
                    break;
                default:
                    if (!inBody)
                        break;
                    var member = _formatChild(child, indentLevel + 1);
                    if (!string.IsNullOrWhiteSpace(member))
                        members.Add(member);
                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + "typeset " + name;
        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", members.Select(m => m.Trim()));
            return header + " { " + inner + " };";
        }

        var lines = new List<string> { header, _layout.Indent(indentLevel) + "{" };
        lines.AddRange(members);
        lines.Add(_layout.Indent(indentLevel) + "};");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatFunctag(Node node, int indentLevel)
    {
        var parts = new List<string> { "functag" };
        foreach (var child in node.Children)
        {
            if (child.Type is "functag" or ";")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.Indent(indentLevel) + _layout.JoinDeclarationParts(parts) + ";";
    }

    private string FormatFuncenum(Node node, int indentLevel)
    {
        var name = "";
        var members = new List<string>();
        var inBody = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "funcenum":
                case ";":
                case ",":
                    break;
                case "identifier":
                    if (!inBody)
                        name = _formatChild(child, 0);
                    break;
                case "{":
                    inBody = true;
                    break;
                case "}":
                    break;
                case "funcenum_member":
                    members.Add(FormatFuncenumMember(child, indentLevel + 1) + ",");
                    break;
                default:
                    if (!inBody)
                        break;
                    var member = _formatChild(child, indentLevel + 1);
                    if (!string.IsNullOrWhiteSpace(member))
                        members.Add(member);
                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + "funcenum " + name;
        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", members.Select(m => m.Trim()));
            return header + " { " + inner + " };";
        }

        var lines = new List<string> { header, _layout.Indent(indentLevel) + "{" };
        lines.AddRange(members);
        lines.Add(_layout.Indent(indentLevel) + "};");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatFuncenumMember(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.Indent(indentLevel) + _layout.JoinDeclarationParts(parts);
    }

    private string FormatStruct(Node node, int indentLevel)
    {
        var name = "";
        var members = new List<string>();
        var inBody = false;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "struct":
                case ";":
                case ",":
                    break;
                case "identifier":
                    if (!inBody)
                        name = _formatChild(child, 0);
                    break;
                case "{":
                    inBody = true;
                    break;
                case "}":
                    break;
                default:
                    if (!inBody)
                        break;
                    var member = _formatChild(child, indentLevel + 1);
                    if (!string.IsNullOrWhiteSpace(member))
                        members.Add(member);
                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + "struct " + name;
        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = string.Join(" ", members.Select(m => m.Trim()));
            return header + " { " + inner + " };";
        }

        var lines = new List<string> { header, _layout.Indent(indentLevel) + "{" };
        lines.AddRange(members);
        lines.Add(_layout.Indent(indentLevel) + "};");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatStructField(Node node, int indentLevel)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type is ";" or ",")
                continue;

            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        var joined = _layout.JoinDeclarationParts(parts);
        if (node.Type == "struct_field" && !joined.EndsWith(';'))
            joined += ";";

        return _layout.Indent(indentLevel) + joined;
    }

    private string FormatStructDeclaration(Node node, int indentLevel)
    {
        var headerParts = new List<string>();
        string? constructor = null;

        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case "=":
                case ";":
                    break;
                case "struct_constructor":
                    constructor = FormatStructConstructor(child, indentLevel);
                    break;
                case ":":
                    // Old-style `Type:name` keeps the colon glued to the type.
                    if (headerParts.Count > 0)
                        headerParts[^1] = headerParts[^1] + ":";
                    break;
                default:
                    var part = _formatChild(child, 0);
                    if (!string.IsNullOrEmpty(part))
                        headerParts.Add(part);
                    break;
            }
        }

        var header = _layout.Indent(indentLevel) + _layout.JoinDeclarationParts(headerParts) + " =";
        if (constructor == null)
            return header + ";";

        if (!_layout.Options.NewLineAfterOpenBrace)
            return header + " " + constructor.TrimStart();

        return header + _layout.Options.LineEnding + constructor;
    }

    private string FormatStructConstructor(Node node, int indentLevel)
    {
        var fields = new List<string>();
        foreach (var child in node.Children)
        {
            if (child.Type is "{" or "}" or "," or ";")
                continue;

            var field = _formatChild(child, indentLevel + 1);
            if (!string.IsNullOrWhiteSpace(field))
                fields.Add(field);
        }

        if (!_layout.Options.NewLineAfterOpenBrace)
        {
            var inner = _layout.JoinComma(fields.Select(f => f.Trim().TrimEnd(',')));
            return "{ " + inner + " };";
        }

        var lines = new List<string> { _layout.Indent(indentLevel) + "{" };
        foreach (var field in fields)
        {
            var line = field.TrimEnd();
            if (!line.EndsWith(','))
                line += ",";
            lines.Add(line);
        }

        lines.Add(_layout.Indent(indentLevel) + "};");
        return string.Join(_layout.Options.LineEnding, lines);
    }

    private string FormatStructFieldValue(Node node, int indentLevel)
    {
        var field = "";
        string? value = null;
        var sawEquals = false;

        foreach (var child in node.Children)
        {
            if (child.Type == "=")
            {
                sawEquals = true;
                continue;
            }

            var formatted = _formatChild(child, 0);
            if (string.IsNullOrEmpty(formatted))
                continue;

            if (!sawEquals)
                field = formatted;
            else
                value = formatted;
        }

        var assignment = field + (_layout.Options.SpaceAroundOperators ? " = " : "=") + (value ?? "");
        return _layout.Indent(indentLevel) + assignment;
    }

    private string FormatArrayType(Node node)
    {
        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, 0);
            if (!string.IsNullOrEmpty(formatted))
                parts.Add(formatted);
        }

        return _layout.JoinDeclarationParts(parts);
    }

    private string FormatSourceFile(Node node, int indentLevel)
    {
        var entries = new List<(string Type, string Text)>();

        foreach (var child in node.Children)
        {
            var formatted = _formatChild(child, indentLevel);
            if (string.IsNullOrWhiteSpace(formatted))
                continue;
            entries.Add((child.Type, formatted));
        }

        if (_layout.Options.SortIncludes)
            SortIncludeRuns(entries);

        var lines = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            lines.Add(entries[i].Text);

            var isInclude = entries[i].Type == "preproc_include";
            var nextIsInclude = i + 1 < entries.Count && entries[i + 1].Type == "preproc_include";
            if (isInclude && !nextIsInclude && _layout.Options.NewLineAfterInclude && i + 1 < entries.Count)
                lines.Add("");
        }

        return CleanUpEmptyLines(string.Join(_layout.Options.LineEnding, lines));
    }

    private static void SortIncludeRuns(List<(string Type, string Text)> entries)
    {
        var i = 0;
        while (i < entries.Count)
        {
            if (entries[i].Type != "preproc_include")
            {
                i++;
                continue;
            }

            var start = i;
            while (i < entries.Count && entries[i].Type == "preproc_include")
                i++;

            var run = entries.GetRange(start, i - start);
            run.Sort((a, b) => string.CompareOrdinal(a.Text, b.Text));
            for (var j = 0; j < run.Count; j++)
                entries[start + j] = run[j];
        }
    }

    private string CleanUpEmptyLines(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        if (!_layout.Options.PreserveEmptyLines)
            return string.Join(_layout.Options.LineEnding, lines.Where(l => !string.IsNullOrWhiteSpace(l)));

        var result = new List<string>();
        var consecutiveEmpty = 0;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                consecutiveEmpty++;
                if (consecutiveEmpty <= _layout.Options.MaxConsecutiveEmptyLines)
                    result.Add("");
            }
            else
            {
                consecutiveEmpty = 0;
                result.Add(line);
            }
        }

        return string.Join(_layout.Options.LineEnding, result);
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

    private string FormatOldTypeCast(Node node)
    {
        // Grammar: old_type + value. Colon stays inside old_type text (Float:0).
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

            if (_layout.IsAssignmentOperator(child.Type))
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
        // tree-sitter-sourcepawn omits the ':' token from ternary children.
        Node? condition = null;
        Node? consequence = null;
        Node? alternative = null;
        var sawQuestion = false;

        foreach (var child in node.Children)
        {
            if (child.Type == "?")
            {
                sawQuestion = true;
                continue;
            }

            if (child.Type == ":")
                continue;

            if (!sawQuestion)
                condition = child;
            else if (consequence == null)
                consequence = child;
            else
                alternative = child;
        }

        var conditionText = condition != null ? _formatChild(condition, 0) : "";
        var consequenceText = consequence != null ? _formatChild(consequence, 0) : "";
        var alternativeText = alternative != null ? _formatChild(alternative, 0) : "";
        var question = _layout.Options.SpaceAroundOperators ? " ? " : "?";
        var colon = _layout.Options.SpaceAroundOperators ? " : " : ":";
        return conditionText + question + consequenceText + colon + alternativeText;
    }
}
