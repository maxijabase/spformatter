using SpFormatter;
using TreeSitter;

namespace SpModernizer;

public sealed class SourcePawnModernizer : IDisposable
{
    private readonly ModernizeOptions _options;
    private readonly SourcePawnParser _parser;
    private bool _disposed;

    public SourcePawnModernizer(ModernizeOptions? options = null)
    {
        _options = options ?? ModernizeOptions.Default;
        _parser = new SourcePawnParser();
    }

    public string Modernize(string sourceCode)
    {
        var result = ModernizeWithResult(sourceCode);
        if (!result.Success)
        {
            var details = string.Join(
                Environment.NewLine + Environment.NewLine,
                result.Errors.Select(e => e.GetDetailedDescription()));
            throw new InvalidOperationException(
                $"Modernize failed:{Environment.NewLine}{Environment.NewLine}{details}");
        }

        return result.Text;
    }

    public ModernizeResult ModernizeWithResult(string sourceCode)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        sourceCode = NormalizeNewlines(sourceCode);

        if (!_options.AllowUnsafeMacros && MacroSafety.ContainsFunctionLikeDefine(sourceCode))
        {
            return ModernizeResult.Fail(
                "Refusing to modernize: function-like #define detected. " +
                "Use AllowUnsafeMacros or --unsafe-macros to override.");
        }

        using var tree = _parser.ParseSource(sourceCode);
        if (tree?.RootNode == null)
            return ModernizeResult.Fail("Unable to parse source code");

        if (tree.RootNode.HasError)
            return ModernizeResult.Fail(_parser.GetSyntaxErrors(sourceCode));

        var enabled = ResolveEnabledRules();
        var context = new RewriteContext
        {
            Source = sourceCode,
            Root = tree.RootNode,
            EnabledRules = enabled,
        };

        foreach (var rule in RuleRegistry.All)
        {
            if (!enabled.Contains(rule.Id))
                continue;
            rule.Apply(context);
        }

        if (!EditApplier.TrySelectNonOverlapping(context.Edits, out var accepted, out var overlapError))
            return ModernizeResult.Fail(overlapError!);

        var rewritten = EditApplier.Apply(sourceCode, accepted);
        var changes = accepted.Select(e => new ModernizeChange
        {
            RuleId = e.RuleId,
            StartIndex = e.StartIndex,
            EndIndex = e.EndIndex,
            Before = sourceCode.AsSpan(e.StartIndex, e.EndIndex - e.StartIndex).ToString(),
            After = e.Replacement,
        }).ToList();

        if (_options.FormatAfter)
        {
            var formatOptions = _options.FormattingOptions ?? FormattingOptions.Default;
            using var formatter = new SourcePawnFormatter(formatOptions);
            var formatResult = formatter.FormatWithResult(rewritten);
            if (!formatResult.Success)
                return ModernizeResult.Fail(formatResult.Errors);

            rewritten = formatResult.Text;
        }

        return ModernizeResult.Ok(rewritten, changes, context.Diagnostics);
    }

    private HashSet<string> ResolveEnabledRules()
    {
        IEnumerable<string> selected = _options.EnabledRules.Count > 0
            ? _options.EnabledRules
            : RuleIds.DefaultEnabled;

        var set = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        foreach (var excluded in _options.ExcludedRules)
            set.Remove(excluded);

        return set;
    }

    private static string NormalizeNewlines(string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode))
            return sourceCode;

        return sourceCode.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _parser.Dispose();
        _disposed = true;
    }
}
