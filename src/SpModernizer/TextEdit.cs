namespace SpModernizer;

internal readonly record struct TextEdit(int StartIndex, int EndIndex, string Replacement, string RuleId)
{
    public int Length => EndIndex - StartIndex;
}

internal static class EditApplier
{
    public static string Apply(string source, IReadOnlyList<TextEdit> edits)
    {
        if (edits.Count == 0)
            return source;

        var sorted = edits.OrderByDescending(e => e.StartIndex).ThenByDescending(e => e.Length).ToList();
        var text = source;
        foreach (var edit in sorted)
        {
            if (edit.StartIndex < 0 || edit.EndIndex > text.Length || edit.StartIndex > edit.EndIndex)
                throw new InvalidOperationException(
                    $"Invalid edit range [{edit.StartIndex}, {edit.EndIndex}) for rule {edit.RuleId}");

            text = string.Concat(
                text.AsSpan(0, edit.StartIndex),
                edit.Replacement,
                text.AsSpan(edit.EndIndex));
        }

        return text;
    }

    /// <summary>
    /// Prefer larger / earlier spans. Reject overlaps fail-closed when <paramref name="failOnOverlap"/> is true.
    /// </summary>
    public static bool TrySelectNonOverlapping(
        IEnumerable<TextEdit> candidates,
        out List<TextEdit> accepted,
        out string? overlapError)
    {
        accepted = new List<TextEdit>();
        overlapError = null;

        var ordered = candidates
            .OrderBy(e => e.StartIndex)
            .ThenByDescending(e => e.Length)
            .ToList();

        foreach (var edit in ordered)
        {
            var overlaps = accepted.Any(a => RangesOverlap(a.StartIndex, a.EndIndex, edit.StartIndex, edit.EndIndex));
            if (overlaps)
            {
                // Larger span already accepted; skip nested candidate.
                var covering = accepted.FirstOrDefault(a =>
                    a.StartIndex <= edit.StartIndex && a.EndIndex >= edit.EndIndex);
                if (covering.RuleId != null)
                    continue;

                overlapError =
                    $"Overlapping modernize edits from rules '{edit.RuleId}' and another rule " +
                    $"at [{edit.StartIndex}, {edit.EndIndex}).";
                accepted = new List<TextEdit>();
                return false;
            }

            accepted.Add(edit);
        }

        return true;
    }

    private static bool RangesOverlap(int aStart, int aEnd, int bStart, int bEnd) =>
        aStart < bEnd && bStart < aEnd;
}
