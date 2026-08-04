using TreeSitter;

namespace SpModernizer;

internal sealed class RewriteContext
{
    public required string Source { get; init; }
    public required Node Root { get; init; }
    public required HashSet<string> EnabledRules { get; init; }
    public List<TextEdit> Edits { get; } = new();
    public List<ModernizeDiagnostic> Diagnostics { get; } = new();

    public bool IsEnabled(string ruleId) => EnabledRules.Contains(ruleId);

    public void AddEdit(Node node, string replacement, string ruleId)
    {
        var before = Source.AsSpan(node.StartIndex, node.EndIndex - node.StartIndex).ToString();
        if (before == replacement)
            return;

        Edits.Add(new TextEdit(node.StartIndex, node.EndIndex, replacement, ruleId));
    }

    public void AddEdit(int start, int end, string replacement, string ruleId)
    {
        var before = Source.AsSpan(start, end - start).ToString();
        if (before == replacement)
            return;

        Edits.Add(new TextEdit(start, end, replacement, ruleId));
    }

    public void AddDiagnostic(string ruleId, string message, Node node) =>
        Diagnostics.Add(new ModernizeDiagnostic
        {
            RuleId = ruleId,
            Message = message,
            StartIndex = node.StartIndex,
            EndIndex = node.EndIndex,
        });
}

internal interface IRewriteRule
{
    string Id { get; }
    void Apply(RewriteContext context);
}
