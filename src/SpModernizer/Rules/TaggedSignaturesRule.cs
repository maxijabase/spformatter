using System.Text;
using TreeSitter;

namespace SpModernizer.Rules;

/// <summary>
/// Old-style tagged function returns and parameters → transitional signatures.
/// Citations: wiki Examples; https://forums.alliedmods.net/showthread.php?t=244092
/// </summary>
internal sealed class TaggedSignaturesRule : IRewriteRule
{
    public string Id => RuleIds.TaggedSignatures;

    public void Apply(RewriteContext context)
    {
        if (!context.IsEnabled(Id))
            return;

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "parameter_declaration"))
            TryRewriteParameter(context, node);

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "function_definition"))
            TryRewriteFunction(context, node);

        foreach (var node in TypeMapper.DescendantsOfType(context.Root, "function_declaration"))
            TryRewriteFunction(context, node);
    }

    private static void TryRewriteParameter(RewriteContext context, Node node)
    {
        // Skip when owned by functag/funcenum whole-node rewrite (overlap resolver).
        Node oldType = null!;
        if (NodeHelpers.TryGetField(node, "type", out var typeNode))
        {
            if (typeNode.Type == "old_type")
                oldType = typeNode;
            else if (typeNode.Type is "type" or "array_type" or "builtin_type")
                return;
            else
            {
                foreach (var child in typeNode.NamedChildren)
                {
                    if (child.Type == "old_type")
                    {
                        oldType = child;
                        break;
                    }
                }
            }
        }

        if (oldType is null)
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
            return;

        var hasModernType = node.NamedChildren.Any(c => c.Type is "type" or "array_type" or "builtin_type");
        if (oldType is null && hasModernType)
            return;

        string modernType;
        if (oldType is not null)
        {
            if (!TypeMapper.TryMapOldType(oldType, out modernType!, out var multi) || multi)
            {
                if (multi)
                {
                    context.AddDiagnostic(
                        RuleIds.MultiTag,
                        "Multi-tag parameter has no transitional equivalent (removed in SourceMod 1.7).",
                        oldType);
                }

                return;
            }
        }
        else
        {
            if (!IsInOldStyleSignatureContext(node))
                return;
            modernType = "int";
        }

        var byRef = node.Children.Any(c => c.Text == "&");
        var storage = string.Empty;
        if (NodeHelpers.TryGetField(node, "storage_class", out var storageNode))
            storage = storageNode.Text.Trim();

        var defaultValue = string.Empty;
        if (NodeHelpers.TryGetField(node, "defaultValue", out var def))
            defaultValue = " = " + NodeHelpers.Slice(context.Source, def);
        else
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (node.Children[i].Text == "=" && i + 1 < node.Children.Count)
                {
                    defaultValue = " = " + NodeHelpers.Slice(context.Source, node.Children[i + 1]);
                    break;
                }
            }
        }

        var dims = DeclaratorDims.Collect(node, context.Source);
        var (typeSuffix, nameSuffix, _) = DeclaratorDims.Format(modernType, dims, DeclaratorDims.Placement.Parameter);

        // Skip no-op for already `int name` produced elsewhere.
        var sb = new StringBuilder();
        if (!string.IsNullOrEmpty(storage))
        {
            sb.Append(storage);
            sb.Append(' ');
        }

        sb.Append(modernType);
        sb.Append(typeSuffix);
        sb.Append(' ');
        if (byRef)
            sb.Append('&');
        sb.Append(nameNode.Text.Trim());
        sb.Append(nameSuffix);
        sb.Append(defaultValue);

        context.AddEdit(node, sb.ToString(), RuleIds.TaggedSignatures);
    }

    private static void TryRewriteFunction(RewriteContext context, Node node)
    {
        if (!NodeHelpers.TryGetField(node, "name", out var nameNode))
            return;

        Node oldReturn = null!;
        var hasModernReturn = false;

        if (NodeHelpers.TryGetField(node, "returnType", out var returnType))
        {
            if (returnType.Type == "old_type")
                oldReturn = returnType;
            else
                hasModernReturn = true;
        }

        if (oldReturn is null)
        {
            foreach (var child in node.NamedChildren)
            {
                if (child.Type == "old_type" && child.StartIndex < nameNode.StartIndex)
                {
                    oldReturn = child;
                    break;
                }
            }
        }

        if (oldReturn is not null)
        {
            if (!TypeMapper.TryMapOldType(oldReturn, out var modernType, out var multi) || multi)
            {
                if (multi)
                {
                    context.AddDiagnostic(
                        RuleIds.MultiTag,
                        "Multi-tag return type has no transitional equivalent (removed in SourceMod 1.7).",
                        oldReturn);
                }

                return;
            }

            context.AddEdit(oldReturn, EnsureTypeSpacing(context.Source, oldReturn, modernType), RuleIds.TaggedSignatures);
            return;
        }

        // BAILOPAN / wiki: omitted return type → void when there is no value return
        // (public OnPluginStart() → public void OnPluginStart()).
        // MenuHandler / SortFunc1D and any function with a valued return need a non-void type.
        if (hasModernReturn)
            return;

        var inferred = InferOmittedReturnType(node, context.Source);
        context.AddEdit(nameNode.StartIndex, nameNode.StartIndex, inferred + " ", RuleIds.TaggedSignatures);
    }

    private static string InferOmittedReturnType(Node functionNode, string source)
    {
        if (LooksLikeMenuHandler(functionNode) || LooksLikeSortFunc1D(functionNode))
            return "int";

        var body = functionNode.NamedChildren.FirstOrDefault(c => c.Type == "block");
        if (body is null)
            return "void";

        var hasValueReturn = false;
        var hasActionReturn = false;
        var hasBoolReturn = false;
        var hasNonBoolReturn = false;

        foreach (var ret in TypeMapper.DescendantsOfType(body, "return_statement"))
        {
            var expr = ret.NamedChildren.FirstOrDefault();
            if (expr is null)
                continue;

            hasValueReturn = true;
            var text = NodeHelpers.Slice(source, expr).Trim();
            if (text.StartsWith("Plugin_", StringComparison.Ordinal))
                hasActionReturn = true;
            else if (expr.Type == "bool_literal" || text is "true" or "false")
                hasBoolReturn = true;
            else
                hasNonBoolReturn = true;
        }

        if (hasActionReturn)
            return "Action";
        if (hasBoolReturn && !hasNonBoolReturn && !hasActionReturn)
            return "bool";
        if (hasValueReturn)
            return "int";
        return "void";
    }

    /// <summary>
    /// Detects CreateMenu / SendPanelToClient style callbacks:
    /// (Handle|Menu, MenuAction, cell, cell).
    /// </summary>
    private static bool LooksLikeMenuHandler(Node functionNode)
    {
        var parameters = GetParameters(functionNode);
        if (parameters.Count != 4)
            return false;

        return ParameterTypeNameIs(parameters[0], "Handle", "Menu")
            && ParameterTypeNameIs(parameters[1], "MenuAction");
    }

    /// <summary>
    /// Detects SortCustom1D callbacks: (elem1, elem2, const array[], Handle).
    /// Citation: SourceMod sorting.inc SortFunc1D.
    /// </summary>
    private static bool LooksLikeSortFunc1D(Node functionNode)
    {
        var parameters = GetParameters(functionNode);
        if (parameters.Count != 4)
            return false;

        // Fourth param is Handle (tagged or modern).
        if (!ParameterTypeNameIs(parameters[3], "Handle"))
            return false;

        // Third param is an array (has dimension).
        var third = parameters[2];
        var hasArrayDim = third.NamedChildren.Any(c => c.Type is "dimension" or "fixed_dimension")
            || (NodeHelpers.TryGetField(third, "type", out var t) && t.Type == "array_type");
        return hasArrayDim;
    }

    private static List<Node> GetParameters(Node functionNode) =>
        functionNode.NamedChildren
            .Where(c => c.Type == "parameter_declarations")
            .SelectMany(p => NodeHelpers.NamedChildrenOfType(p, "parameter_declaration"))
            .ToList();

    private static bool ParameterTypeNameIs(Node parameter, params string[] names)
    {
        if (NodeHelpers.TryGetField(parameter, "type", out var typeNode))
        {
            var mapped = TypeNameOf(typeNode);
            if (names.Any(n => string.Equals(mapped, n, StringComparison.Ordinal)))
                return true;
        }

        foreach (var child in parameter.NamedChildren)
        {
            if (child.Type != "old_type")
                continue;

            var mapped = TypeNameOf(child);
            if (names.Any(n => string.Equals(mapped, n, StringComparison.Ordinal)))
                return true;
        }

        return false;
    }

    private static string TypeNameOf(Node typeNode)
    {
        if (typeNode.Type == "old_type")
        {
            foreach (var child in typeNode.NamedChildren)
            {
                if (child.Type is "identifier" or "old_builtin_type")
                    return child.Text.Trim();
            }
        }

        if (typeNode.Type is "type" or "builtin_type")
        {
            var id = typeNode.NamedChildren.FirstOrDefault(c => c.Type is "identifier" or "builtin_type");
            if (id is not null)
                return id.Text.Trim();
            return typeNode.Text.Trim();
        }

        if (typeNode.Type == "identifier")
            return typeNode.Text.Trim();

        return typeNode.Text.Trim().TrimEnd(':');
    }

    private static bool IsInOldStyleSignatureContext(Node param)
    {
        var parent = param.Parent;
        while (parent is not null)
        {
            if (parent.Type is "functag" or "funcenum_member" or "funcenum")
                return true;

            if (parent.Type is "function_definition" or "function_declaration")
            {
                if (parent.NamedChildren.Any(c => c.Type == "old_type"))
                    return true;

                if (NodeHelpers.TryGetField(parent, "returnType", out var rt))
                {
                    if (rt.Type == "old_type")
                        return true;
                    if (rt.Type is "type" or "builtin_type")
                        return false;
                }

                return true;
            }

            parent = parent.Parent;
        }

        return false;
    }

    private static string EnsureTypeSpacing(string source, Node oldType, string modernType)
    {
        if (oldType.EndIndex < source.Length)
        {
            var next = source[oldType.EndIndex];
            if (!char.IsWhiteSpace(next))
                return modernType + " ";
        }

        return modernType;
    }
}
