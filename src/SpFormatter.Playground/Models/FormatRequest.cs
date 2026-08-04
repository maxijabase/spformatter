using SpFormatter;

namespace SpFormatter.Playground.Models;

public sealed class FormatRequest
{
    public string? Source { get; set; }
    public FormatOptionsDto? Options { get; set; }
}

public sealed class FormatOptionsDto
{
    public int? IndentSize { get; set; }
    public bool? UseSpaces { get; set; }
    public bool? SpaceAfterComma { get; set; }
    public bool? SpaceAroundOperators { get; set; }
    public bool? SpaceBeforeOpenParen { get; set; }
    public bool? SpaceInArrayBrackets { get; set; }
    public bool? NewLineAfterOpenBrace { get; set; }
    public bool? NewLineAfterInclude { get; set; }
    public bool? PreserveEmptyLines { get; set; }
    public int? MaxConsecutiveEmptyLines { get; set; }
    public bool? SortIncludes { get; set; }
    public bool? RequireSemicolons { get; set; }
    public bool? AllowSyntaxRecovery { get; set; }
    public bool? AllowUnsafeMacros { get; set; }
    public string? LineEnding { get; set; }

    public FormattingOptions ToFormattingOptions()
    {
        var defaults = FormattingOptions.Default;
        return new FormattingOptions
        {
            IndentSize = IndentSize ?? defaults.IndentSize,
            UseSpaces = UseSpaces ?? defaults.UseSpaces,
            SpaceAfterComma = SpaceAfterComma ?? defaults.SpaceAfterComma,
            SpaceAroundOperators = SpaceAroundOperators ?? defaults.SpaceAroundOperators,
            SpaceBeforeOpenParen = SpaceBeforeOpenParen ?? defaults.SpaceBeforeOpenParen,
            SpaceInArrayBrackets = SpaceInArrayBrackets ?? defaults.SpaceInArrayBrackets,
            NewLineAfterOpenBrace = NewLineAfterOpenBrace ?? defaults.NewLineAfterOpenBrace,
            NewLineAfterInclude = NewLineAfterInclude ?? defaults.NewLineAfterInclude,
            PreserveEmptyLines = PreserveEmptyLines ?? defaults.PreserveEmptyLines,
            MaxConsecutiveEmptyLines = MaxConsecutiveEmptyLines ?? defaults.MaxConsecutiveEmptyLines,
            SortIncludes = SortIncludes ?? defaults.SortIncludes,
            RequireSemicolons = RequireSemicolons ?? defaults.RequireSemicolons,
            AllowSyntaxRecovery = AllowSyntaxRecovery ?? false,
            AllowUnsafeMacros = AllowUnsafeMacros ?? false,
            LineEnding = NormalizeLineEnding(LineEnding)
        };
    }

    private static string NormalizeLineEnding(string? value) =>
        value switch
        {
            "\r\n" or "crlf" or "CRLF" => "\r\n",
            "\n" or "lf" or "LF" => "\n",
            null or "" => "\n",
            _ => value
        };
}
