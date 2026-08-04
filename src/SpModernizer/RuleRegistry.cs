using SpModernizer.Rules;

namespace SpModernizer;

internal static class RuleRegistry
{
    public static IReadOnlyList<IRewriteRule> All { get; } =
    [
        new OldTypeCastRule(),
        new OldBuiltinsRule(),
        new OldVariablesRule(),
        new TaggedSignaturesRule(),
        new FunctagRule(),
        new FuncenumRule(),
        new OldStructFieldsRule(),
        new LegacyWhileRule(),
        new OldTypesRule(),
        new MultiTagRule(),
    ];
}
