using TreeSitter;

namespace SpModernizer;

/// <summary>
/// Modernizes expression subtrees so parent rewrites (e.g. old-variables) do not
/// preserve nested legacy casts like Float:fval.
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#View_As
/// </summary>
internal static class ExpressionModernizer
{
    public static string ModernizeSpan(string source, Node root)
    {
        var text = NodeHelpers.Slice(source, root);
        var casts = TypeMapper.DescendantsOfType(root, "old_type_cast")
            .OrderByDescending(c => c.StartIndex)
            .ThenByDescending(c => c.EndIndex - c.StartIndex)
            .ToList();

        foreach (var cast in casts)
        {
            if (cast.StartIndex < root.StartIndex || cast.EndIndex > root.EndIndex)
                continue;

            if (!TryFormatCast(source, cast, out var replacement))
                continue;

            var localStart = cast.StartIndex - root.StartIndex;
            var localEnd = cast.EndIndex - root.StartIndex;
            text = string.Concat(text.AsSpan(0, localStart), replacement, text.AsSpan(localEnd));
        }

        return text;
    }

    public static bool TryFormatCast(string source, Node cast, out string replacement)
    {
        replacement = string.Empty;
        if (!NodeHelpers.TryGetField(cast, "type", out var typeNode)
            || !NodeHelpers.TryGetField(cast, "value", out var valueNode))
            return false;

        if (!TypeMapper.TryMapOldType(typeNode, out var modernType, out var multiTag) || multiTag)
            return false;

        // Value side may itself contain nested casts.
        var valueText = ModernizeSpan(source, valueNode);
        replacement = $"view_as<{modernType}>({valueText})";
        return true;
    }
}
