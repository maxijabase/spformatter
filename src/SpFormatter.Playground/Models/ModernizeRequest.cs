using SpFormatter;
using SpModernizer;

namespace SpFormatter.Playground.Models;

public sealed class ModernizeRequest
{
    public string? Source { get; set; }
    public bool FormatAfter { get; set; } = true;
    public FormatOptionsDto? Options { get; set; }
    public string[]? EnabledRules { get; set; }
    public string[]? ExcludedRules { get; set; }
    public bool AllowUnsafeMacros { get; set; }
}

public sealed class ModernizeResponse
{
    public bool Success { get; init; }
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<ModernizeChangeDto> Changes { get; init; } = Array.Empty<ModernizeChangeDto>();
    public IReadOnlyList<ModernizeDiagnosticDto> Diagnostics { get; init; } = Array.Empty<ModernizeDiagnosticDto>();
    public IReadOnlyList<FormatErrorDto> Errors { get; init; } = Array.Empty<FormatErrorDto>();

    public static ModernizeResponse FromResult(ModernizeResult result) =>
        new()
        {
            Success = result.Success,
            Text = result.Text,
            Changes = result.Changes.Select(c => new ModernizeChangeDto
            {
                RuleId = c.RuleId,
                StartIndex = c.StartIndex,
                EndIndex = c.EndIndex,
                Before = c.Before,
                After = c.After,
            }).ToArray(),
            Diagnostics = result.Diagnostics.Select(d => new ModernizeDiagnosticDto
            {
                RuleId = d.RuleId,
                Message = d.Message,
                StartIndex = d.StartIndex,
                EndIndex = d.EndIndex,
            }).ToArray(),
            Errors = result.Errors.Select(FormatErrorDto.From).ToArray(),
        };
}

public sealed class ModernizeChangeDto
{
    public string RuleId { get; init; } = string.Empty;
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
    public string Before { get; init; } = string.Empty;
    public string After { get; init; } = string.Empty;
}

public sealed class ModernizeDiagnosticDto
{
    public string RuleId { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int StartIndex { get; init; }
    public int EndIndex { get; init; }
}
