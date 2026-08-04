using TreeSitter;

namespace SpModernizer;

internal static class NodeHelpers
{
    public static bool TryGetField(Node node, string fieldName, out Node child)
    {
        child = null!;
        var found = node.GetChildForField(fieldName);
        if (found is null || found.Id == IntPtr.Zero || string.IsNullOrEmpty(found.Type))
            return false;

        child = found;
        return true;
    }

    public static IReadOnlyList<Node> GetFields(Node node, string fieldName) =>
        node.GetChildrenForField(fieldName) ?? Array.Empty<Node>();

    public static string Slice(string source, Node node) =>
        source.AsSpan(node.StartIndex, node.EndIndex - node.StartIndex).ToString();

    public static string Slice(string source, int start, int end) =>
        source.AsSpan(start, end - start).ToString();

    public static bool HasAncestorOfType(Node node, string type)
    {
        var parent = node.Parent;
        while (parent is not null && parent.Id != IntPtr.Zero && !string.IsNullOrEmpty(parent.Type))
        {
            if (parent.Type == type)
                return true;
            parent = parent.Parent;
        }

        return false;
    }

    public static IEnumerable<Node> NamedChildrenOfType(Node node, string type) =>
        node.NamedChildren.Where(c => c.Type == type);

    public static string? FormatInitializer(string source, Node decl)
    {
        var parts = GetFields(decl, "initialValue");
        if (parts.Count == 0)
        {
            for (var i = 0; i < decl.Children.Count; i++)
            {
                if (decl.Children[i].Type == "=" && i + 1 < decl.Children.Count)
                    return " = " + ExpressionModernizer.ModernizeSpan(source, decl.Children[i + 1]);
            }

            return null;
        }

        // Grammar binds both "=" and the expression as initialValue.
        var expr = parts.FirstOrDefault(p => p.Type != "=");
        if (expr is null)
            return null;

        return " = " + ExpressionModernizer.ModernizeSpan(source, expr);
    }
}
