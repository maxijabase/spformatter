using SpFormatter;

namespace SpFormatter.Playground.Models;

public sealed class FormatResponse
{
    public bool Success { get; init; }
    public string Text { get; init; } = string.Empty;
    public IReadOnlyList<FormatErrorDto> Errors { get; init; } = Array.Empty<FormatErrorDto>();

    public static FormatResponse FromResult(FormatResult result) =>
        new()
        {
            Success = result.Success,
            Text = result.Text,
            Errors = result.Errors.Select(FormatErrorDto.From).ToArray()
        };
}

public sealed class FormatErrorDto
{
    public string Message { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int StartColumn { get; init; }
    public int EndLine { get; init; }
    public int EndColumn { get; init; }
    public string NodeType { get; init; } = string.Empty;
    public bool IsMissing { get; init; }

    public static FormatErrorDto From(SyntaxError error) =>
        new()
        {
            Message = error.Message,
            StartLine = error.StartLine,
            StartColumn = error.StartColumn,
            EndLine = error.EndLine,
            EndColumn = error.EndColumn,
            NodeType = error.NodeType,
            IsMissing = error.IsMissing
        };
}
