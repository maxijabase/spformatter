using System.Text;
using TreeSitter;

namespace SpModernizer.Rules;

/// <summary>
/// functag → typedef.
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#Typedefs
/// </summary>
internal sealed class FunctagRule : IRewriteRule
{
    public string Id => RuleIds.Functag;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "functag"))
        {
            if (!TryRewrite(context, node, out var replacement))
                continue;
            context.AddEdit(node, replacement, Id);
        }
    }

    private static bool TryRewrite(RewriteContext context, Node node, out string replacement)
    {
        replacement = string.Empty;

        if (!NodeHelpers.TryGetField(node, "name", out var nameNode))
            return false;
        if (!NodeHelpers.TryGetField(node, "parameters", out var paramsNode))
            return false;

        var returnType = "void";
        if (NodeHelpers.TryGetField(node, "returnType", out var rt) && rt.Type == "old_type")
        {
            if (!TypeMapper.TryMapOldType(rt, out returnType, out var multi) || multi)
                return false;
        }
        else
        {
            foreach (var child in node.NamedChildren)
            {
                if (child.Type == "old_type")
                {
                    if (!TypeMapper.TryMapOldType(child, out returnType, out var multi) || multi)
                        return false;
                    break;
                }
            }
        }

        if (!TryModernizeParameterList(context, paramsNode, out var modernParams))
            return false;

        var name = nameNode.Text.Trim();
        replacement = $"typedef {name} = function {returnType} ({modernParams});";
        return true;
    }

    internal static bool TryModernizeParameterList(RewriteContext context, Node paramsNode, out string modernParams)
    {
        modernParams = string.Empty;
        var parts = new List<string>();

        foreach (var child in paramsNode.NamedChildren)
        {
            if (child.Type == "rest_parameter")
            {
                parts.Add(ModernizeRest(context, child));
                continue;
            }

            if (child.Type != "parameter_declaration")
                continue;

            if (TryFormatParam(context, child, out var text))
                parts.Add(text);
            else
                return false;
        }

        modernParams = string.Join(", ", parts);
        return true;
    }

    private static string ModernizeRest(RewriteContext context, Node node)
    {
        if (NodeHelpers.TryGetField(node, "type", out var typeNode))
        {
            if (typeNode.Type == "old_type"
                && TypeMapper.TryMapOldType(typeNode, out var modern, out var multi)
                && !multi)
                return modern + " ...";

            return NodeHelpers.Slice(context.Source, typeNode).TrimEnd(':') + " ...";
        }

        return "...";
    }

    private static bool TryFormatParam(RewriteContext context, Node node, out string text)
    {
        text = string.Empty;

        Node? typeNode = null;
        var hasTypeField = NodeHelpers.TryGetField(node, "type", out var foundType);
        if (hasTypeField)
            typeNode = foundType;

        if (hasTypeField && typeNode is not null && typeNode.Type is "type" or "array_type")
        {
            text = NodeHelpers.Slice(context.Source, node).Trim();
            return true;
        }

        Node? oldType = null;
        if (hasTypeField && typeNode is not null && typeNode.Type == "old_type")
            oldType = typeNode;
        else
        {
            foreach (var child in node.NamedChildren)
            {
                if (child.Type == "old_type")
                {
                    oldType = child;
                    break;
                }
            }
        }

        if (!NodeHelpers.TryGetField(node, "name", out var nameNode))
            return false;

        var modernType = "int";
        if (oldType is not null)
        {
            if (!TypeMapper.TryMapOldType(oldType, out modernType!, out var multi) || multi)
                return false;
        }

        var storage = string.Empty;
        if (NodeHelpers.TryGetField(node, "storage_class", out var storageNode))
            storage = storageNode.Text.Trim() + " ";

        var dims = DeclaratorDims.Collect(node, context.Source);
        var (typeSuffix, nameSuffix, _) = DeclaratorDims.Format(modernType, dims, DeclaratorDims.Placement.Parameter);

        var byRef = node.Children.Any(c => c.Text == "&");
        var sb = new StringBuilder();
        sb.Append(storage);
        sb.Append(modernType);
        sb.Append(typeSuffix);
        sb.Append(' ');
        if (byRef)
            sb.Append('&');
        sb.Append(nameNode.Text.Trim());
        sb.Append(nameSuffix);
        text = sb.ToString();
        return true;
    }
}

/// <summary>
/// funcenum → typeset.
/// Citation: https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#Typedefs
/// </summary>
internal sealed class FuncenumRule : IRewriteRule
{
    public string Id => RuleIds.Funcenum;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "funcenum"))
        {
            if (!TryRewrite(context, node, out var replacement))
                continue;
            context.AddEdit(node, replacement, Id);
        }
    }

    private static bool TryRewrite(RewriteContext context, Node node, out string replacement)
    {
        replacement = string.Empty;
        if (!NodeHelpers.TryGetField(node, "name", out var nameNode))
            return false;

        var members = new List<string>();
        foreach (var member in NodeHelpers.NamedChildrenOfType(node, "funcenum_member"))
        {
            if (!TryRewriteMember(context, member, out var memberText))
                return false;
            members.Add(memberText);
        }

        if (members.Count == 0)
            return false;

        var sb = new StringBuilder();
        sb.Append("typeset ");
        sb.Append(nameNode.Text.Trim());
        sb.AppendLine(" {");
        for (var i = 0; i < members.Count; i++)
        {
            sb.Append("  ");
            sb.Append(members[i]);
            sb.AppendLine(";");
        }

        sb.Append("};");
        replacement = sb.ToString();
        return true;
    }

    private static bool TryRewriteMember(RewriteContext context, Node member, out string text)
    {
        text = string.Empty;
        var returnType = "void";
        if (NodeHelpers.TryGetField(member, "returnType", out var rt) && rt.Type == "old_type")
        {
            if (!TypeMapper.TryMapOldType(rt, out returnType, out var multi) || multi)
                return false;
        }
        else
        {
            foreach (var child in member.NamedChildren)
            {
                if (child.Type != "old_type")
                    continue;
                if (!TypeMapper.TryMapOldType(child, out returnType, out var multi) || multi)
                    return false;
                break;
            }
        }

        if (!NodeHelpers.TryGetField(member, "parameters", out var paramsNode))
            return false;

        if (!FunctagRule.TryModernizeParameterList(context, paramsNode, out var modernParams))
            return false;

        text = $"function {returnType} ({modernParams})";
        return true;
    }
}
