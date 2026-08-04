using SpFormatter;

namespace SpModernizer;

public sealed class ModernizeResult
{
    public bool Success { get; }
    public string Text { get; }
    public IReadOnlyList<ModernizeChange> Changes { get; }
    public IReadOnlyList<ModernizeDiagnostic> Diagnostics { get; }
    public IReadOnlyList<SyntaxError> Errors { get; }

    private ModernizeResult(
        bool success,
        string text,
        IReadOnlyList<ModernizeChange> changes,
        IReadOnlyList<ModernizeDiagnostic> diagnostics,
        IReadOnlyList<SyntaxError> errors)
    {
        Success = success;
        Text = text;
        Changes = changes;
        Diagnostics = diagnostics;
        Errors = errors;
    }

    public static ModernizeResult Ok(
        string text,
        IReadOnlyList<ModernizeChange>? changes = null,
        IReadOnlyList<ModernizeDiagnostic>? diagnostics = null) =>
        new(
            true,
            text,
            changes ?? Array.Empty<ModernizeChange>(),
            diagnostics ?? Array.Empty<ModernizeDiagnostic>(),
            Array.Empty<SyntaxError>());

    public static ModernizeResult Fail(IReadOnlyList<SyntaxError> errors) =>
        new(false, string.Empty, Array.Empty<ModernizeChange>(), Array.Empty<ModernizeDiagnostic>(), errors);

    public static ModernizeResult Fail(string message) =>
        Fail(new[] { new SyntaxError { Message = message } });
}

public sealed class ModernizeChange
{
    public required string RuleId { get; init; }
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
    public required string Before { get; init; }
    public required string After { get; init; }
}

public sealed class ModernizeDiagnostic
{
    public required string RuleId { get; init; }
    public required string Message { get; init; }
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
}
