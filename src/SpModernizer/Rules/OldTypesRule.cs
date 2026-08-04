using TreeSitter;

namespace SpModernizer.Rules;

/// <summary>
/// Remaining old_type nodes (not owned by a larger rewrite) → modern type text.
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#New_Declarators
/// </summary>
internal sealed class OldTypesRule : IRewriteRule
{
    public string Id => RuleIds.OldTypes;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        var remapBuiltins = context.IsEnabled(RuleIds.OldBuiltins);

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "old_type"))
        {
            if (!TypeMapper.TryMapOldType(node, out var modernType, out var multiTag) || multiTag)
            {
                if (multiTag)
                {
                    context.AddDiagnostic(
                        RuleIds.MultiTag,
                        "Multi-tag type has no transitional equivalent (removed in SourceMod 1.7). See https://wiki.alliedmods.net/SourceMod_1.7.0_Release_Notes",
                        node);
                }

                continue;
            }

            if (!remapBuiltins)
            {
                // Without old-builtins, only strip the colon for non-builtin tags.
                var raw = node.Text.Trim().TrimEnd(':');
                if (raw is "Float" or "String" or "_" or "bool" or "void")
                    continue;
                modernType = raw;
            }

            context.AddEdit(node, EnsureTypeSpacing(context.Source, node, modernType), Id);
        }
    }

    private static string EnsureTypeSpacing(string source, Node oldType, string modernType)
    {
        if (oldType.EndIndex < source.Length && !char.IsWhiteSpace(source[oldType.EndIndex]))
            return modernType + " ";
        return modernType;
    }
}

/// <summary>
/// Enables builtin remapping as a selectable rule id. Actual remaps live in <see cref="TypeMapper"/>.
/// </summary>
internal sealed class OldBuiltinsRule : IRewriteRule
{
    public string Id => RuleIds.OldBuiltins;

    public void Apply(RewriteContext context)
    {
        // Builtin remapping is consumed by other rules via EnabledRules.
        // Standalone leaf rewrite of old_builtin_type would leave a trailing colon.
    }
}

/// <summary>
/// Reports multi_tag nodes.
/// Citation: https://wiki.alliedmods.net/SourceMod_1.7.0_Release_Notes (multiple tag support removed)
/// </summary>
internal sealed class MultiTagRule : IRewriteRule
{
    public string Id => RuleIds.MultiTag;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "multi_tag"))
        {
            context.AddDiagnostic(
                Id,
                "Multi-tag syntax has no transitional equivalent (removed in SourceMod 1.7). See https://wiki.alliedmods.net/SourceMod_1.7.0_Release_Notes",
                node);
        }
    }
}
