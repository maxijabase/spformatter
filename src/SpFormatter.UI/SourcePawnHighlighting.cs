using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;

namespace SpFormatter.UI;

/// <summary>
/// AvalonEdit highlighting adapted from SPCode's AeonEditorHighlighting
/// (https://github.com/SPCodeOrg/SPCode). Stock keywords only; no SM include DB.
/// </summary>
public sealed class SourcePawnHighlighting : IHighlightingDefinition
{
    private readonly HighlightingRuleSet _mainRuleSet;

    private SourcePawnHighlighting(HighlightingRuleSet mainRuleSet)
    {
        _mainRuleSet = mainRuleSet;
        NamedHighlightingColors = Array.Empty<HighlightingColor>();
    }

    public string Name => "SourcePawn";

    public HighlightingRuleSet MainRuleSet => _mainRuleSet;

    public IEnumerable<HighlightingColor> NamedHighlightingColors { get; }

    public IDictionary<string, string> Properties { get; } = new Dictionary<string, string>
    {
        ["DocCommentMarker"] = "///"
    };

    public HighlightingRuleSet? GetNamedRuleSet(string name) => null;

    public HighlightingColor? GetNamedColor(string name) => null;

    public static SourcePawnHighlighting CreateDark()
    {
        // Defaults from SPCode OptionsControl.NormalizeSHColors
        var comments = Brush(0x57, 0xA6, 0x49);
        var commentMarkers = Brush(0xFF, 0x20, 0x20);
        var strings = Brush(0xF4, 0x6B, 0x6C);
        var preprocessor = Brush(0x7E, 0x7E, 0x7E);
        var types = Brush(0x28, 0x90, 0xB0);
        var typeValues = Brush(0x56, 0x9C, 0xD5);
        var keywords = Brush(0x56, 0x9C, 0xD5);
        var contextKeywords = Brush(0x56, 0x9C, 0xD5);
        var chars = Brush(0xD6, 0x9C, 0x85);
        var unknownFunctions = Brush(0x45, 0x85, 0xC5);
        var numbers = Brush(0x97, 0x97, 0x97);
        var special = Brush(0x8F, 0x8F, 0x8F);
        var deprecated = Brush(0xFF, 0x00, 0x00);

        return new SourcePawnHighlighting(BuildRuleSet(
            comments, commentMarkers, strings, preprocessor, types, typeValues,
            keywords, contextKeywords, chars, unknownFunctions, numbers, special, deprecated));
    }

    public static SourcePawnHighlighting CreateLight()
    {
        var comments = Brush(0x5B, 0x7A, 0x4E);
        var commentMarkers = Brush(0xC0, 0x28, 0x28);
        var strings = Brush(0xA1, 0x5C, 0x38);
        var preprocessor = Brush(0x6B, 0x5B, 0x8A);
        var types = Brush(0x0F, 0x7A, 0x6B);
        var typeValues = Brush(0x1F, 0x5F, 0x99);
        var keywords = Brush(0x1F, 0x5F, 0x99);
        var contextKeywords = Brush(0x1F, 0x5F, 0x99);
        var chars = Brush(0x8A, 0x5A, 0x3A);
        var unknownFunctions = Brush(0x7A, 0x5A, 0x12);
        var numbers = Brush(0x3F, 0x6B, 0x4A);
        var special = Brush(0x3A, 0x46, 0x54);
        var deprecated = Brush(0xC0, 0x28, 0x28);

        return new SourcePawnHighlighting(BuildRuleSet(
            comments, commentMarkers, strings, preprocessor, types, typeValues,
            keywords, contextKeywords, chars, unknownFunctions, numbers, special, deprecated));
    }

    private static HighlightingRuleSet BuildRuleSet(
        HighlightingBrush comments,
        HighlightingBrush commentMarkers,
        HighlightingBrush strings,
        HighlightingBrush preprocessor,
        HighlightingBrush types,
        HighlightingBrush typeValues,
        HighlightingBrush keywords,
        HighlightingBrush contextKeywords,
        HighlightingBrush chars,
        HighlightingBrush unknownFunctions,
        HighlightingBrush numbers,
        HighlightingBrush special,
        HighlightingBrush deprecated)
    {
        var commentMarkerSet = new HighlightingRuleSet { Name = "CommentMarkerSet" };
        commentMarkerSet.Rules.Add(new HighlightingRule
        {
            Regex = KeywordRegex("TODO", "FIX", "FIXME", "HACK", "WORKAROUND", "BUG"),
            Color = new HighlightingColor
            {
                Foreground = commentMarkers,
                FontWeight = FontWeights.Bold
            }
        });

        var stringEscapeSet = new HighlightingRuleSet();
        stringEscapeSet.Spans.Add(new HighlightingSpan
        {
            StartExpression = new Regex(@"\\", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            EndExpression = new Regex(@".", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture)
        });

        var rs = new HighlightingRuleSet { Name = "MainRule" };

        rs.Spans.Add(new HighlightingSpan
        {
            StartExpression = new Regex(@"//", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            EndExpression = new Regex(@"$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            SpanColor = new HighlightingColor { Foreground = comments },
            StartColor = new HighlightingColor { Foreground = comments },
            EndColor = new HighlightingColor { Foreground = comments },
            RuleSet = commentMarkerSet
        });

        rs.Spans.Add(new HighlightingSpan
        {
            StartExpression = new Regex(@"/\*", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            EndExpression = new Regex(@"\*/", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            SpanColor = new HighlightingColor { Foreground = comments },
            StartColor = new HighlightingColor { Foreground = comments },
            EndColor = new HighlightingColor { Foreground = comments },
            RuleSet = commentMarkerSet
        });

        rs.Spans.Add(new HighlightingSpan
        {
            StartExpression = new Regex(@"(?<!')""", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            EndExpression = new Regex(@"""", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            SpanColor = new HighlightingColor { Foreground = strings },
            StartColor = new HighlightingColor { Foreground = strings },
            EndColor = new HighlightingColor { Foreground = strings },
            RuleSet = stringEscapeSet
        });

        rs.Rules.Add(Rule(@"\b(decl|String|Float|functag|funcenum)\b", deprecated));
        rs.Rules.Add(Rule(@"\#\S+", preprocessor));
        rs.Rules.Add(Rule(@"(?<=#pragma deprecated ).+", strings));
        rs.Rules.Add(Rule(KeywordRegex("sizeof", "true", "false", "null"), typeValues));
        rs.Rules.Add(Rule(
            KeywordRegex(
                "if", "else", "switch", "case", "default", "for", "while", "do", "break", "continue",
                "return", "new", "view_as", "delete"),
            keywords));
        rs.Rules.Add(Rule(
            KeywordRegex(
                "stock", "normal", "native", "public", "static", "const", "methodmap", "enum", "forward",
                "function", "struct", "property", "get", "set", "typeset", "typedef", "this", "operator",
                "private"),
            contextKeywords));
        rs.Rules.Add(Rule(
            KeywordRegex("bool", "char", "float", "int", "void", "any", "Handle", "Function", "Action", "Plugin"),
            types));
        rs.Rules.Add(Rule(@"'\\?.?'", chars));
        rs.Rules.Add(Rule(
            @"\b0[xX][0-9a-fA-F]+\b|\b0[bB][01]+\b|\b0[oO][0-7]+\b|\b[0-9]+(\.[0-9]+)?([eE][+-]?[0-9]+)?\b",
            numbers));
        rs.Rules.Add(Rule(@"\s<[A-Za-z0-9_\\/\-]+(\.[A-Za-z0-9_\-]+)?>", strings));
        rs.Rules.Add(Rule(@"[?.;()\[\]{}+\-/%*&<>^~!|]+", special));
        rs.Rules.Add(Rule(@"(?<!#define )\b\w+(?=\s*\()", unknownFunctions));

        return rs;
    }

    private static HighlightingRule Rule(string pattern, HighlightingBrush brush) =>
        new()
        {
            Regex = new Regex(pattern, RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture),
            Color = new HighlightingColor { Foreground = brush }
        };

    private static HighlightingRule Rule(Regex regex, HighlightingBrush brush) =>
        new()
        {
            Regex = regex,
            Color = new HighlightingColor { Foreground = brush }
        };

    private static Regex KeywordRegex(params string[] keywords) =>
        new(
            $@"\b({string.Join("|", keywords.Select(Regex.Escape))})\b",
            RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    private static SimpleHighlightingBrush Brush(byte r, byte g, byte b) =>
        new(Color.FromRgb(r, g, b));
}

public sealed class SimpleHighlightingBrush : HighlightingBrush
{
    private readonly SolidColorBrush _brush;

    public SimpleHighlightingBrush(Color color)
    {
        _brush = new SolidColorBrush(color);
        _brush.Freeze();
    }

    public override Brush GetBrush(ITextRunConstructionContext context) => _brush;

    public override string ToString() => _brush.ToString();
}
