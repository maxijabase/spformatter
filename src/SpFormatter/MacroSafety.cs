using System.Text.RegularExpressions;

namespace SpFormatter;

/// <summary>
/// Detects preprocessor shapes the AST printer cannot safely rewrite.
/// Function-like macros can inject braces/control flow; formatting the unexpanded
/// file may invent semicolons or otherwise break compiling plugins.
/// </summary>
public static partial class MacroSafety
{
    public const string RefusalMessage =
        "Refusing to format: function-like #define detected. " +
        "SpFormatter formats SourcePawn, not arbitrary preprocessor rewrites. " +
        "Use AllowUnsafeMacros or --unsafe-macros to override.";

    /// <summary>
    /// True when the source contains a C/SP-style function-like define:
    /// <c>#define Name(</c> with no space between the name and <c>(</c>.
    /// Object-like defines such as <c>#define FOO (1+2)</c> do not match.
    /// </summary>
    public static bool ContainsFunctionLikeDefine(string source) =>
        !string.IsNullOrEmpty(source) && FunctionLikeDefinePattern().IsMatch(source);

    [GeneratedRegex(@"^\s*#\s*define\s+\w+\(", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex FunctionLikeDefinePattern();
}
