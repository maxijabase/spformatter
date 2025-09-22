using TreeSitter;

namespace SpFormatter;

public class SourcePawnParser : IDisposable
{
    private readonly Language _language;
    private readonly Parser _parser;
    private bool _disposed;

    public SourcePawnParser()
    {
        try
        {
            _language = new Language("tree-sitter-sourcepawn", "tree_sitter_sourcepawn");
            _parser = new Parser(_language);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to initialize SourcePawn parser. Ensure tree-sitter-sourcepawn.dll is available.", ex);
        }
    }

    public Tree? ParseSource(string sourceCode)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SourcePawnParser));

        if (string.IsNullOrEmpty(sourceCode))
            return null;

        try
        {
            return _parser.Parse(sourceCode);
        }
        catch (Exception ex)
        {
            throw new FormatException($"Failed to parse SourcePawn code: {ex.Message}", ex);
        }
    }

    public bool IsValidSyntax(string sourceCode)
    {
        using var tree = ParseSource(sourceCode);
        return tree?.RootNode != null && !tree.RootNode.HasError;
    }

    public List<SyntaxError> GetSyntaxErrors(string sourceCode)
    {
        var errors = new List<SyntaxError>();
        using var tree = ParseSource(sourceCode);
        
        if (tree?.RootNode != null)
        {
            CollectSyntaxErrors(tree.RootNode, sourceCode, errors);
        }
        
        return errors;
    }

    private void CollectSyntaxErrors(Node node, string sourceCode, List<SyntaxError> errors)
    {
        if (node.IsError)
        {
            var lines = sourceCode.Split('\n');
            var startPos = node.StartPosition;
            var endPos = node.EndPosition;
            
            errors.Add(new SyntaxError
            {
                Message = $"Syntax error at '{node.Text}'",
                StartLine = (int)startPos.Row + 1,
                StartColumn = (int)startPos.Column + 1,
                EndLine = (int)endPos.Row + 1,
                EndColumn = (int)endPos.Column + 1,
                StartIndex = node.StartIndex,
                EndIndex = node.EndIndex,
                NodeType = node.Type,
                Context = GetErrorContext(sourceCode, startPos, endPos)
            });
        }

        if (node.IsMissing)
        {
            var startPos = node.StartPosition;
            errors.Add(new SyntaxError
            {
                Message = $"Missing syntax element: expected '{node.Type}'",
                StartLine = (int)startPos.Row + 1,
                StartColumn = (int)startPos.Column + 1,
                EndLine = (int)startPos.Row + 1,
                EndColumn = (int)startPos.Column + 1,
                StartIndex = node.StartIndex,
                EndIndex = node.EndIndex,
                NodeType = node.Type,
                IsMissing = true,
                Context = GetErrorContext(sourceCode, startPos, startPos)
            });
        }

        foreach (var child in node.Children)
        {
            CollectSyntaxErrors(child, sourceCode, errors);
        }
    }

    private string GetErrorContext(string sourceCode, Point startPos, Point endPos)
    {
        var lines = sourceCode.Split('\n');
        var startLine = (int)startPos.Row;
        var endLine = (int)endPos.Row;
        
        // Get context lines (1 before, error line(s), 1 after)
        var contextStart = Math.Max(0, startLine - 1);
        var contextEnd = Math.Min(lines.Length - 1, endLine + 1);
        
        var context = new List<string>();
        for (int i = contextStart; i <= contextEnd; i++)
        {
            var prefix = (i == startLine) ? ">>> " : "    ";
            context.Add($"{i + 1:D3}| {prefix}{lines[i]}");
        }
        
        return string.Join("\n", context);
    }

    public void PrintSyntaxTree(string sourceCode)
    {
        using var tree = ParseSource(sourceCode);
        if (tree?.RootNode != null)
        {
            Console.WriteLine("Syntax Tree:");
            Console.WriteLine(tree.RootNode.Expression);
        }
    }

    public void WalkTree(Node node, int depth = 0)
    {
        var indent = new string(' ', depth * 2);
        Console.WriteLine($"{indent}{node.Type}: {node.Text}");
        
        foreach (var child in node.Children)
        {
            WalkTree(child, depth + 1);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _parser?.Dispose();
            _language?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
