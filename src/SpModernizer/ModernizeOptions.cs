namespace SpModernizer;

public sealed class ModernizeOptions
{
    public static ModernizeOptions Default => new();

    /// <summary>
    /// Rule ids to run. Empty means all registered default-enabled rules.
    /// </summary>
    public IReadOnlyList<string> EnabledRules { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Rule ids to skip even when otherwise enabled.
    /// </summary>
    public IReadOnlyList<string> ExcludedRules { get; init; } = Array.Empty<string>();

    /// <summary>
    /// When true, run <see cref="SpFormatter.SourcePawnFormatter"/> after rewrites.
    /// Library default is false so tests assert dialect output only.
    /// </summary>
    public bool FormatAfter { get; init; }

    public SpFormatter.FormattingOptions? FormattingOptions { get; init; }

    public bool AllowUnsafeMacros { get; init; }
}
