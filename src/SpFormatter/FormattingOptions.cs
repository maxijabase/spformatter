namespace SpFormatter;

public class FormattingOptions
{
    public int IndentSize { get; set; } = 4;
    public bool UseSpaces { get; set; } = true;
    public string IndentString => UseSpaces ? new string(' ', IndentSize) : "\t";
    
    public bool SpaceAfterComma { get; set; } = true;
    public bool SpaceAroundOperators { get; set; } = true;
    public bool SpaceBeforeOpenParen { get; set; } = false;
    public bool SpaceAfterSemicolon { get; set; } = true;
    
    public bool NewLineAfterOpenBrace { get; set; } = true;
    public bool NewLineBeforeCloseBrace { get; set; } = true;
    public bool NewLineAfterSemicolon { get; set; } = true;
    
    public int MaxLineLength { get; set; } = 120;
    
    // Advanced formatting options
    public bool PreserveEmptyLines { get; set; } = true;
    public int MaxConsecutiveEmptyLines { get; set; } = 2;
    public bool SortIncludes { get; set; } = false;
    public bool IndentPreprocessor { get; set; } = false;
    public bool AlignConsecutiveAssignments { get; set; } = false;
    public bool AlignConsecutiveDeclarations { get; set; } = false;
    
    // SourcePawn-specific options
    public bool SpaceInArrayBrackets { get; set; } = false;
    public bool NewLineAfterInclude { get; set; } = true;
    public bool CompactFunctionParameters { get; set; } = false;
    
    // Semicolon handling options
    public bool RequireSemicolons { get; set; } = true;  // true = enforce semicolons, false = #pragma semicolon 0 style
    public bool RemoveOptionalSemicolons { get; set; } = false;  // when RequireSemicolons = false, remove existing ones
    
    // Line ending options
    public string LineEnding { get; set; } = Environment.NewLine;  // Use platform-specific line endings
    
    public static FormattingOptions Default => new();
}
