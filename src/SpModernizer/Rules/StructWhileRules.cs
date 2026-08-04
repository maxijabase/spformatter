using System.Text;
using TreeSitter;

namespace SpModernizer.Rules;

/// <summary>
/// Legacy comma-separated old_struct_field list → modern public typed fields.
/// </summary>
internal sealed class OldStructFieldsRule : IRewriteRule
{
    public string Id => RuleIds.OldStructFields;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        var structs = TypeMapper.DescendantsOfType(context.Root, "struct")
            .Where(s => s.NamedChildren.Any(c => c.Type == "old_struct_field"))
            .ToList();

        foreach (var node in structs)
        {
            if (!TryRewriteStruct(context, node, out var replacement))
                continue;
            context.AddEdit(node, replacement, Id);
        }
    }

    private static bool TryRewriteStruct(RewriteContext context, Node node, out string replacement)
    {
        replacement = string.Empty;
        var name = node.NamedChildren.FirstOrDefault(c => c.Type == "identifier");
        if (name is null)
            return false;

        var fields = new List<string>();
        foreach (var field in NodeHelpers.NamedChildrenOfType(node, "old_struct_field"))
        {
            if (!TryFormatField(context, field, out var text))
                return false;
            fields.Add(text);
        }

        if (fields.Count == 0)
            return false;

        var sb = new StringBuilder();
        sb.Append("struct ");
        sb.Append(name.Text.Trim());
        sb.AppendLine(" {");
        foreach (var field in fields)
        {
            sb.Append("    ");
            sb.Append(field);
            sb.AppendLine(";");
        }

        sb.Append("};");
        replacement = sb.ToString();
        return true;
    }

    private static bool TryFormatField(RewriteContext context, Node node, out string text)
    {
        text = string.Empty;
        if (!NodeHelpers.TryGetField(node, "name", out var nameNode))
            return false;

        var isConst = node.Children.Any(c => c.Text == "const");
        var typeName = "int";
        if (NodeHelpers.TryGetField(node, "type", out var typeNode) && typeNode.Type == "old_type")
        {
            if (!TypeMapper.TryMapOldType(typeNode, out typeName, out var multi) || multi)
                return false;
        }

        var dims = DeclaratorDims.Collect(node, context.Source);
        var (typeSuffix, nameSuffix, _) = DeclaratorDims.Format(
            typeName,
            dims,
            DeclaratorDims.Placement.Parameter);

        var sb = new StringBuilder("public ");
        if (isConst)
            sb.Append("const ");
        sb.Append(typeName);
        sb.Append(typeSuffix);
        sb.Append(' ');
        sb.Append(nameNode.Text.Trim());
        sb.Append(nameSuffix);
        text = sb.ToString();
        return true;
    }
}

/// <summary>
/// Legacy while !cond do stmt → while (cond) stmt; bare do-while conditions get parens.
/// </summary>
internal sealed class LegacyWhileRule : IRewriteRule
{
    public string Id => RuleIds.LegacyWhile;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "while_statement"))
            TryRewriteWhile(context, node);

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "do_while_statement"))
            TryRewriteDoWhile(context, node);
    }

    private static void TryRewriteWhile(RewriteContext context, Node node)
    {
        var hasDo = node.Children.Any(c => c.Text == "do");
        var hasParen = node.Children.Any(c => c.Text == "(");
        if (!hasDo || hasParen)
            return;

        if (!NodeHelpers.TryGetField(node, "condition", out var condition))
            return;
        if (!NodeHelpers.TryGetField(node, "body", out var body))
            return;

        var condText = NodeHelpers.Slice(context.Source, condition);
        var bodyText = NodeHelpers.Slice(context.Source, body);
        context.AddEdit(node, $"while ({condText})\n{bodyText}", RuleIds.LegacyWhile);
    }

    private static void TryRewriteDoWhile(RewriteContext context, Node node)
    {
        var hasParen = false;
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i].Text == "while" && i + 1 < node.Children.Count && node.Children[i + 1].Text == "(")
            {
                hasParen = true;
                break;
            }
        }

        if (hasParen)
            return;

        if (!NodeHelpers.TryGetField(node, "condition", out var condition))
            return;
        if (!NodeHelpers.TryGetField(node, "body", out var body))
            return;

        var bodyText = NodeHelpers.Slice(context.Source, body).TrimEnd();
        var condText = NodeHelpers.Slice(context.Source, condition);
        var semi = node.Children.Any(c => c.Text == ";") ? ";" : "";
        context.AddEdit(node, $"do\n{bodyText}\nwhile ({condText}){semi}", RuleIds.LegacyWhile);
    }
}
