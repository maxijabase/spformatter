using TreeSitter;

namespace SpModernizer.Rules;

/// <summary>
/// Retag expressions → view_as&lt;T&gt;(expr).
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#View_As
/// </summary>
internal sealed class OldTypeCastRule : IRewriteRule
{
    public string Id => RuleIds.OldTypeCast;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "old_type_cast"))
        {
            // Parent statement rewrites (old-variables) already inline-modernize nested casts.
            if (NodeHelpers.HasAncestorOfType(node, "old_variable_declaration")
                || NodeHelpers.HasAncestorOfType(node, "old_variable_declaration_statement")
                || NodeHelpers.HasAncestorOfType(node, "old_global_variable_declaration")
                || NodeHelpers.HasAncestorOfType(node, "old_for_loop_variable_declaration_statement"))
                continue;

            if (!NodeHelpers.TryGetField(node, "type", out var typeNode))
                continue;

            if (!TypeMapper.TryMapOldType(typeNode, out _, out var multiTag) || multiTag)
            {
                if (multiTag)
                {
                    context.AddDiagnostic(
                        RuleIds.MultiTag,
                        "Multi-tag cast has no transitional equivalent (removed in SourceMod 1.7). See https://wiki.alliedmods.net/SourceMod_1.7.0_Release_Notes",
                        typeNode);
                }

                continue;
            }

            if (!ExpressionModernizer.TryFormatCast(context.Source, node, out var replacement))
                continue;

            context.AddEdit(node, replacement, Id);
        }
    }
}
