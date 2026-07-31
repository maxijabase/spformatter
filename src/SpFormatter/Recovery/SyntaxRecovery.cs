using System.Text.RegularExpressions;
using TreeSitter;

namespace SpFormatter.Recovery;

/// <summary>
/// Opt-in ERROR-tree and expression-wrapper recovery. Must not run on clean trees by default.
/// </summary>
public sealed class SyntaxRecovery
{
    private readonly FormattingOptions _options;
    private readonly SourcePawnParser _parser;
    private readonly Func<Node, int, string?, string> _formatNode;

    public SyntaxRecovery(
        FormattingOptions options,
        SourcePawnParser parser,
        Func<Node, int, string?, string> formatNode)
    {
        _options = options;
        _parser = parser;
        _formatNode = formatNode;
    }

    public bool IsExpressionOnlyFormatting(Node rootNode, string sourceCode)
    {
        var trimmed = sourceCode.Trim();

        if (trimmed.StartsWith("int dummy = ") || trimmed.StartsWith("void dummy() { "))
            return true;

        if (!trimmed.EndsWith(";"))
        {
            var topLevelNodes = rootNode.Children.Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();

            var isSimpleExpression = topLevelNodes.Any(child =>
                child.Type == "assignment_expression" ||
                child.Type == "binary_expression" ||
                child.Type == "call_expression" ||
                child.Type == "array_indexed_access" ||
                child.Type == "update_expression" ||
                child.Type == "global_variable_declaration" ||
                child.Type == "old_global_variable_declaration" ||
                child.Type == "old_variable_declaration" ||
                child.Type.Contains("expression"));

            var hasStatements = topLevelNodes.Any(child =>
                child.Type.Contains("statement") ||
                child.Type.Contains("function_definition") ||
                child.Type.Contains("preprocessor"));

            return isSimpleExpression && !hasStatements;
        }

        return false;
    }

    public string? TryFormatAsExpression(string sourceCode)
    {
        var trimmed = sourceCode.Trim();

        if (trimmed.StartsWith("if(") || trimmed.StartsWith("if ") ||
            trimmed.StartsWith("for(") || trimmed.StartsWith("for ") ||
            trimmed.StartsWith("while(") || trimmed.StartsWith("while ") ||
            trimmed.StartsWith("switch(") || trimmed.StartsWith("switch "))
        {
            return null;
        }

        string[] wrappers =
        {
            $"int dummy = {sourceCode};",
            $"void dummy() {{ {sourceCode}; }}",
            $"void dummy() {{ func({sourceCode}); }}"
        };

        foreach (var wrapper in wrappers)
        {
            try
            {
                using var tree = _parser.ParseSource(wrapper);
                if (tree?.RootNode != null && !tree.RootNode.HasError)
                {
                    var formatted = _formatNode(tree.RootNode, 0, wrapper);
                    var extracted = ExtractFormattedExpression(formatted);
                    if (extracted != null)
                        return extracted;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    public string AddSpacesAroundBinaryOperators(string text)
    {
        if (!_options.SpaceAroundOperators)
            return RemoveSpacesAroundUnaryOperators(text);

        var binaryOperators = new[]
        {
            "<<", ">>", "==", "!=", "<=", ">=", "&&", "||",
            "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=",
            "+", "-", "*", "/", "%", "="
        };

        foreach (var op in binaryOperators)
        {
            if (op.Length == 1)
            {
                var multiCharOps = new[]
                {
                    "&&", "||", "++", "--", "<<", ">>", "==", "!=", "<=", ">=",
                    "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^="
                };
                var hasConflict = multiCharOps.Any(multiOp => multiOp.Contains(op) && text.Contains(multiOp));
                if (hasConflict)
                    continue;
            }

            var pattern = $@"(\S)({Regex.Escape(op)})(\s*)(\S)";
            text = Regex.Replace(text, pattern, "$1 $2 $4");
        }

        return RemoveSpacesAroundUnaryOperators(text);
    }

    private static string RemoveSpacesAroundUnaryOperators(string text)
    {
        text = Regex.Replace(text, @"(\w)\s+(\+\+)", "$1$2");
        text = Regex.Replace(text, @"(\-\-)\s+(\w)", "$1$2");
        text = Regex.Replace(text, @"(\+\+)\s+(\w)", "$1$2");
        text = Regex.Replace(text, @"(\w)\s+(\-\-)", "$1$2");
        text = Regex.Replace(text, @"(!\s+)(\w)", "!$2");
        text = Regex.Replace(text, @"(\+\+)\s*\r?\n\s*(\w)", "$1$2");
        text = Regex.Replace(text, @"(\-\-)\s*\r?\n\s*(\w)", "$1$2");
        text = Regex.Replace(text, @"(!\s*\r?\n\s*)(\w)", "!$2");
        text = Regex.Replace(text, @"(\+\+)\s+(\w)", "$1$2");
        text = Regex.Replace(text, @"(\-\-)\s+(\w)", "$1$2");
        return text;
    }

    private static string? ExtractFormattedExpression(string formattedWrapper)
    {
        var lines = formattedWrapper.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("void ") || trimmed.StartsWith("{") || trimmed.StartsWith("}") ||
                trimmed.StartsWith("int ") || trimmed.StartsWith("if ") || string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (trimmed.Contains(" = "))
            {
                var equalIndex = trimmed.IndexOf(" = ", StringComparison.Ordinal);
                var afterEqual = trimmed[(equalIndex + 3)..];
                if (afterEqual.EndsWith(';'))
                    afterEqual = afterEqual[..^1];
                return afterEqual;
            }

            if (trimmed.EndsWith(';'))
                return trimmed[..^1];

            if (!string.IsNullOrEmpty(trimmed))
                return trimmed;
        }

        return null;
    }
}
