using TreeSitter;

namespace SpModernizer;

/// <summary>
/// Places array brackets per transitional rules.
/// Citations:
/// https://wiki.alliedmods.net/SourcePawn_Transitional_Syntax#Arrays
/// (fixed dims after name; dynamic dims after type; no dims on both;
/// non-constant sizes become <c>T[] name = new T[expr]</c>).
/// </summary>
internal static class DeclaratorDims
{
    internal enum Placement
    {
        /// <summary>Variable / field decls. Empty-only dims stay after the name (<c>char buf[]</c>) so globals parse.</summary>
        Variable,

        /// <summary>Parameters prefer empty dims on the type (<c>char[] name</c>).</summary>
        Parameter,
    }

    internal readonly record struct Dim(string Text, bool IsEmpty, bool IsConstantFixed);

    public static List<Dim> Collect(Node owner, string source)
    {
        var dims = new List<Dim>();
        foreach (var child in owner.NamedChildren)
        {
            if (child.Type == "dimension")
            {
                dims.Add(new Dim("[]", IsEmpty: true, IsConstantFixed: true));
            }
            else if (child.Type == "fixed_dimension")
            {
                dims.Add(new Dim(
                    NodeHelpers.Slice(source, child),
                    IsEmpty: false,
                    IsConstantFixed: IsConstantFixedDimension(child)));
            }
        }

        return dims;
    }

    public static bool IsConstantFixedDimension(Node fixedDimension)
    {
        var expr = fixedDimension.NamedChildren.FirstOrDefault();
        return expr is not null && IsConstantExpr(expr);
    }

    public static bool IsConstantExpr(Node node)
    {
        switch (node.Type)
        {
            case "int_literal":
            case "binary_literal":
            case "hex_literal":
            case "octal_literal":
            case "sizeof_expression":
                return true;
            case "identifier":
                return IsMacroLikeIdentifier(node.Text.Trim());
            case "parenthesized_expression":
                return node.NamedChildren.Count > 0 && node.NamedChildren.All(IsConstantExpr);
            case "binary_expression":
            case "unary_expression":
            case "update_expression":
                return node.NamedChildren.Count > 0 && node.NamedChildren.All(IsConstantExpr);
            default:
                return false;
        }
    }

    /// <summary>
    /// Macro-style identifiers such as MAXPLAYERS are treated as compile-time sizes.
    /// Mixed-case names like MaxClients are runtime values (dynamic arrays).
    /// </summary>
    public static bool IsMacroLikeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var hasLetter = false;
        foreach (var c in name)
        {
            if (c is >= 'A' and <= 'Z')
            {
                hasLetter = true;
                continue;
            }

            if (c is '_' or >= '0' and <= '9')
                continue;

            return false;
        }

        return hasLetter;
    }

    /// <summary>
    /// Returns type suffix (new-dims), name suffix (old-dims), and optional
    /// dynamic initializer replacing a non-constant fixed size.
    /// </summary>
    public static (string TypeSuffix, string NameSuffix, string? DynamicInit) Format(
        string baseType,
        IReadOnlyList<Dim> dims,
        Placement placement = Placement.Variable)
    {
        if (dims.Count == 0)
            return (string.Empty, string.Empty, null);

        var hasDynamic = dims.Any(d => !d.IsEmpty && !d.IsConstantFixed);
        var hasEmpty = dims.Any(d => d.IsEmpty);
        var hasFixed = dims.Any(d => !d.IsEmpty);

        if (hasDynamic)
        {
            // Wiki: int[] players = new int[MaxClients + 1];
            if (dims.Count == 1 && !dims[0].IsEmpty)
            {
                return ("[]", string.Empty, " = new " + baseType + dims[0].Text);
            }

            // Mixed dynamic shapes are uncommon; keep all brackets after the name.
            return (string.Empty, string.Concat(dims.Select(d => d.Text)), null);
        }

        // char[] name[4] is illegal; char name[4][] keeps all old-dims after the name.
        if (hasEmpty && hasFixed)
            return (string.Empty, string.Concat(dims.Select(d => d.Text)), null);

        if (hasEmpty)
        {
            var empty = string.Concat(dims.Select(d => d.Text));
            if (placement == Placement.Parameter)
                return (empty, string.Empty, null);

            // Globals/locals without init parse as char buf[], not char[] buf.
            return (string.Empty, empty, null);
        }

        return (string.Empty, string.Concat(dims.Select(d => d.Text)), null);
    }
}
