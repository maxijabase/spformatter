using System.Text;
using TreeSitter;

namespace SpModernizer.Rules;

/// <summary>
/// Old new/decl variable statements → transitional declarators.
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#New_Declarators
/// Does not rewrite modern new_expression / dynamic array new.
/// </summary>
internal sealed class OldVariablesRule : IRewriteRule
{
    public string Id => RuleIds.OldVariables;

    private static readonly HashSet<string> StatementTypes =
    [
        "old_variable_declaration_statement",
        "old_global_variable_declaration",
        "old_for_loop_variable_declaration_statement",
    ];

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var typeName in StatementTypes)
        {
            foreach (var node in TypeMapper.DescendantsOfType(context.Root, typeName))
            {
                if (!TryRewriteStatement(context, node, out var replacement, out var usedDecl))
                    continue;

                context.AddEdit(node, replacement, Id);
                if (usedDecl)
                {
                    context.AddDiagnostic(
                        Id,
                        "Converted decl to a new-style declaration. New-style declarations are always zero-initialized; decl has no transitional equivalent. See https://wiki.alliedmods.net/SourceMod_1.7.0_Release_Notes",
                        node);
                }
            }
        }
    }

    private static bool TryRewriteStatement(
        RewriteContext context,
        Node node,
        out string replacement,
        out bool usedDecl)
    {
        replacement = string.Empty;
        usedDecl = false;

        var source = context.Source;
        var decls = NodeHelpers.NamedChildrenOfType(node, "old_variable_declaration").ToList();
        if (decls.Count == 0)
            return false;

        var visibility = string.Empty;
        var storage = string.Empty;
        foreach (var child in node.Children)
        {
            if (child.Type == "visibility")
                visibility = child.Text.Trim();
            else if (child.Type == "variable_storage_class")
                storage = child.Text.Trim();
            else if (child.Type is "new" or "decl")
            {
                if (child.Type == "decl")
                    usedDecl = true;
            }
            else if (child.IsNamed == false && (child.Text == "new" || child.Text == "decl"))
            {
                if (child.Text == "decl")
                    usedDecl = true;
            }
        }

        // Also detect decl from raw prefix text before first decl.
        var firstDeclStart = decls[0].StartIndex;
        var prefix = NodeHelpers.Slice(source, node.StartIndex, firstDeclStart);
        if (prefix.Contains("decl", StringComparison.Ordinal))
            usedDecl = true;

        var modernDecls = new List<(string Type, string Text)>();
        foreach (var decl in decls)
        {
            if (!TryFormatDeclarator(context, decl, out var typeName, out var declarator))
                return false;
            modernDecls.Add((typeName, declarator));
        }

        var needsSemicolon = node.Type != "old_for_loop_variable_declaration_statement";
        var prefixParts = new List<string>();
        if (!string.IsNullOrEmpty(visibility))
            prefixParts.Add(visibility);
        if (!string.IsNullOrEmpty(storage))
            prefixParts.Add(storage);

        // Group consecutive same-type declarators.
        var sb = new StringBuilder();
        var i = 0;
        while (i < modernDecls.Count)
        {
            var typeName = modernDecls[i].Type;
            var group = new List<string> { modernDecls[i].Text };
            var j = i + 1;
            while (j < modernDecls.Count && modernDecls[j].Type == typeName)
            {
                group.Add(modernDecls[j].Text);
                j++;
            }

            if (sb.Length > 0)
                sb.AppendLine();

            foreach (var part in prefixParts)
            {
                sb.Append(part);
                sb.Append(' ');
            }

            sb.Append(typeName);
            sb.Append(' ');
            sb.Append(string.Join(", ", group));
            if (needsSemicolon)
                sb.Append(';');

            i = j;
        }

        replacement = sb.ToString();
        return true;
    }

    private static bool TryFormatDeclarator(
        RewriteContext context,
        Node decl,
        out string typeName,
        out string declarator)
    {
        typeName = "int";
        declarator = string.Empty;

        if (!NodeHelpers.TryGetField(decl, "name", out var nameNode))
            return false;

        if (NodeHelpers.TryGetField(decl, "type", out var typeNode))
        {
            if (!TypeMapper.TryMapOldType(typeNode, out typeName, out var multi) || multi)
            {
                if (multi)
                {
                    context.AddDiagnostic(
                        RuleIds.MultiTag,
                        "Multi-tag declaration has no transitional equivalent (removed in SourceMod 1.7).",
                        typeNode);
                }

                return false;
            }
        }

        var name = nameNode.Text.Trim();
        var init = NodeHelpers.FormatInitializer(context.Source, decl) ?? string.Empty;
        var dims = DeclaratorDims.Collect(decl, context.Source);
        var (typeSuffix, nameSuffix, dynamicInit) = DeclaratorDims.Format(typeName, dims);
        typeName += typeSuffix;

        if (dynamicInit is not null)
        {
            // Prefer dynamic allocation; drop a conflicting brace initializer.
            declarator = name + dynamicInit;
            return true;
        }

        declarator = name + nameSuffix + init;
        return true;
    }
}
