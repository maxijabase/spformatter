namespace SpFormatter;

public sealed class FormatResult
{
    public bool Success { get; }
    public string Text { get; }
    public IReadOnlyList<SyntaxError> Errors { get; }

    private FormatResult(bool success, string text, IReadOnlyList<SyntaxError> errors)
    {
        Success = success;
        Text = text;
        Errors = errors;
    }

    public static FormatResult Ok(string text) =>
        new(true, text, Array.Empty<SyntaxError>());

    public static FormatResult Fail(IReadOnlyList<SyntaxError> errors) =>
        new(false, string.Empty, errors);

    public static FormatResult Fail(string message) =>
        Fail(new[]
        {
            new SyntaxError { Message = message }
        });
}
