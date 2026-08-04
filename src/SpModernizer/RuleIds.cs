namespace SpModernizer;

public static class RuleIds
{
    public const string OldTypeCast = "old-type-cast";
    public const string OldBuiltins = "old-builtins";
    public const string OldTypes = "old-types";
    public const string OldVariables = "old-variables";
    public const string TaggedSignatures = "tagged-signatures";
    public const string MultiTag = "multi-tag";
    public const string Functag = "functag";
    public const string Funcenum = "funcenum";
    public const string OldStructFields = "old-struct-fields";
    public const string LegacyWhile = "legacy-while";

    public static IReadOnlyList<string> DefaultEnabled { get; } =
    [
        OldTypeCast,
        OldBuiltins,
        OldTypes,
        OldVariables,
        TaggedSignatures,
        MultiTag,
        Functag,
        Funcenum,
        OldStructFields,
        LegacyWhile,
    ];
}
