using SpFormatter.Legacy;
using SpFormatter.Recovery;
using TreeSitter;

namespace SpFormatter;

public class SourcePawnFormatter : IDisposable
{
    private readonly SourcePawnParser _parser;
    private readonly FormattingOptions _options;
    private readonly LayoutRules _layout;
    private readonly AstPrinter _astPrinter;
    private readonly UnknownNodePrinter _unknownNodePrinter;
    private readonly SyntaxRecovery _recovery;
    private bool _disposed;

    public SourcePawnFormatter(FormattingOptions? options = null)
    {
        _parser = new SourcePawnParser();
        _options = options ?? FormattingOptions.Default;
        _layout = new LayoutRules(_options);
        _astPrinter = new AstPrinter(_layout, (node, indent) => FormatNode(node, indent));
        _unknownNodePrinter = new UnknownNodePrinter(_layout, (node, indent) => FormatNode(node, indent));
        _recovery = new SyntaxRecovery(_options, _parser, FormatNode);
    }

    public string Format(string sourceCode)
    {
        var result = FormatWithResult(sourceCode);
        if (!result.Success)
        {
            var errorDetails = string.Join(
                _options.LineEnding + _options.LineEnding,
                result.Errors.Select(e => e.GetDetailedDescription()));
            throw new FormatException(
                $"Source code contains syntax errors:{_options.LineEnding}{_options.LineEnding}{errorDetails}");
        }

        return result.Text;
    }

    /// <summary>
    /// Formats source and returns a structured result. Prefer this when callers want errors without exceptions.
    /// </summary>
    public FormatResult FormatWithResult(string sourceCode)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SourcePawnFormatter));

        // Tree-sitter treats CR-only files as one long line, so `http://` in strings
        // becomes string + `//` comment and whole files collapse into ERROR/comment soup.
        sourceCode = NormalizeNewlines(sourceCode);

        if (!_options.AllowUnsafeMacros && MacroSafety.ContainsFunctionLikeDefine(sourceCode))
            return FormatResult.Fail(MacroSafety.RefusalMessage);

        using var tree = _parser.ParseSource(sourceCode);
        if (tree?.RootNode == null)
            return FormatResult.Fail("Unable to parse source code");

        _astPrinter.BeginDocument(sourceCode);

        if (tree.RootNode.HasError)
        {
            if (!_options.AllowSyntaxRecovery)
                return FormatResult.Fail(_parser.GetSyntaxErrors(sourceCode));

            try
            {
                var malformedResult = FormatNode(tree.RootNode, 0, sourceCode);
                if (!string.IsNullOrEmpty(malformedResult))
                {
                    if (_recovery.IsExpressionOnlyFormatting(tree.RootNode, sourceCode)
                        && !sourceCode.TrimEnd().EndsWith(";")
                        && malformedResult.TrimEnd().EndsWith(";"))
                    {
                        malformedResult = malformedResult.TrimEnd().TrimEnd(';');
                    }

                    return FormatResult.Ok(malformedResult);
                }
            }
            catch
            {
            }

            var expressionResult = _recovery.TryFormatAsExpression(sourceCode);
            if (expressionResult != null)
                return FormatResult.Ok(expressionResult);

            return FormatResult.Fail(_parser.GetSyntaxErrors(sourceCode));
        }

        var text = FormatNode(tree.RootNode, 0, sourceCode);

        if (_options.AllowSyntaxRecovery
            && _recovery.IsExpressionOnlyFormatting(tree.RootNode, sourceCode)
            && !sourceCode.TrimEnd().EndsWith(";")
            && text.TrimEnd().EndsWith(";"))
        {
            text = text.TrimEnd().TrimEnd(';');
        }

        return FormatResult.Ok(text);
    }

    private string FormatNode(Node node, int indentLevel, string? originalSource = null)
    {
        if (_astPrinter.TryPrint(node, indentLevel, out var printed))
            return printed;

        return node.Type switch
        {
            "source_file" => FormatSourceFileRecovery(node, indentLevel),
            _ => _unknownNodePrinter.Print(node, indentLevel)
        };
    }

    private string FormatSourceFileRecovery(Node node, int indentLevel)
    {
        if (node.HasError && node.Children.Count == 2)
        {
            var first = node.Children[0];
            var second = node.Children[1];

            if (first.Type == "ERROR"
                && (first.Text.Trim() is "++" or "--" or "!")
                && (second.Type is "old_global_variable_declaration" or "global_variable_declaration")
                && second.Children.Count == 1
                && second.Children[0].Type == "old_variable_declaration"
                && second.Children[0].Children.Count == 1
                && System.Text.RegularExpressions.Regex.IsMatch(
                    second.Children[0].Children[0].Text.Trim(), @"^\w+$"))
            {
                return FormatNode(first, indentLevel) + FormatNode(second, indentLevel);
            }

            if (first.Type == "ERROR"
                && first.Text.Trim() == "("
                && second.Type == "global_variable_declaration")
            {
                return _recovery.AddSpacesAroundBinaryOperators(
                    FormatNode(first, indentLevel) + FormatNode(second, indentLevel));
            }
        }

        var parts = new List<string>();
        foreach (var child in node.Children)
        {
            var formatted = FormatNode(child, indentLevel);
            if (!string.IsNullOrWhiteSpace(formatted))
                parts.Add(formatted);
        }

        return string.Join(_options.LineEnding, parts);
    }

    private static string NormalizeNewlines(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            return sourceCode;

        return sourceCode.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _parser?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}
