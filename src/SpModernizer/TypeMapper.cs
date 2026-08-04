using SpFormatter;
using TreeSitter;

namespace SpModernizer;

/// <summary>
/// Maps legacy tags to transitional types.
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#New_Declarators
/// </summary>
internal static class TypeMapper
{
    public static string MapBuiltin(string text) => text switch
    {
        "Float" => "float",
        "String" => "char",
        "_" => "int",
        "bool" => "bool",
        "void" => "void",
        _ => text,
    };

    public static bool TryMapOldType(Node oldTypeNode, out string modernType, out bool isMultiTag)
    {
        modernType = string.Empty;
        isMultiTag = false;

        if (oldTypeNode.Type != "old_type")
            return false;

        foreach (var child in oldTypeNode.NamedChildren)
        {
            switch (child.Type)
            {
                case "multi_tag":
                    isMultiTag = true;
                    return false;
                case "old_builtin_type":
                    modernType = MapBuiltin(child.Text.Trim());
                    return true;
                case "any_type":
                    modernType = "any";
                    return true;
                case "identifier":
                    modernType = child.Text.Trim();
                    return true;
            }
        }

        // Fallback: strip trailing colon from full text (Float:)
        var raw = oldTypeNode.Text.Trim();
        if (raw.EndsWith(':'))
            raw = raw[..^1];

        if (raw.StartsWith('{'))
        {
            isMultiTag = true;
            return false;
        }

        modernType = MapBuiltin(raw);
        return !string.IsNullOrEmpty(modernType);
    }

    public static string MapOldTypeOrThrow(Node oldTypeNode)
    {
        if (!TryMapOldType(oldTypeNode, out var modern, out var multi) || multi)
            throw new InvalidOperationException("Cannot map multi-tag or unknown old_type.");

        return modern;
    }

    public static IEnumerable<Node> DescendantsOfType(Node root, string type)
    {
        foreach (var node in AstInspector.FindNodesByType(root, type))
            yield return node;
    }
}
