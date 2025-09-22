namespace SpFormatter;

public class SyntaxError
{
    public string Message { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int StartColumn { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public string NodeType { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public bool IsMissing { get; set; }
    
    public override string ToString()
    {
        var errorType = IsMissing ? "Missing" : "Error";
        return $"[{errorType}] Line {StartLine}:{StartColumn} - {Message}";
    }
    
    public string GetDetailedDescription()
    {
        var description = $"{this}\n";
        description += $"Node Type: {NodeType}\n";
        description += $"Position: Line {StartLine}, Column {StartColumn} to Line {EndLine}, Column {EndColumn}\n";
        description += $"Character Index: {StartIndex} to {EndIndex}\n";
        if (!string.IsNullOrEmpty(Context))
        {
            description += $"Context:\n{Context}";
        }
        return description;
    }
}
