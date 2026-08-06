#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>ADR 0136/0138 Glass port — contiguous + multi-bracket ordinals (Avalonia-free; CIDE ParametricSegmentListParser mechanics).</summary>
public static class GlassIntercomMessageSelect
{
    public readonly record struct Range(int Start, int End)
    {
        public int InclusiveCount => End - Start + 1;
    }

    public readonly record struct Selection(int ActiveOrdinal, IReadOnlySet<int> Highlighted);

    public static readonly Selection Empty = new(0, new HashSet<int>());

    public static bool IsClear(string? argsTail) =>
        string.Equals((argsTail ?? "").Trim(), "clear", StringComparison.OrdinalIgnoreCase);

    /// <summary>Legacy contiguous «N» / «N M» / «N:M».</summary>
    public static bool TryParseRange(string? argsTail, out int start, out int end, out string error)
    {
        start = 0;
        end = 0;
        if (!TryParseSegments(argsTail, out var segments, out error))
            return false;
        if (segments.Count != 1)
        {
            error = segments.Count == 0
                ? "empty range"
                : "use [3;5] [8;15] for disjoint; contiguous is N · N:M";
            return false;
        }

        start = segments[0].Start;
        end = segments[0].End;
        return true;
    }

    /// <summary>Parse «5», «5 10», «5:10» or «[3;5] [8;15] [20]» — same as CIDE ParametricSegmentListParser.</summary>
    public static bool TryParseSegments(string? argsTail, out IReadOnlyList<Range> segments, out string error)
    {
        segments = [];
        error = "";
        var tail = (argsTail ?? "").Trim();
        if (tail.Length == 0)
        {
            error = "usage: /intercom message select N · N:M · [3;5] [8;15] · clear";
            return false;
        }

        if (IsClear(tail))
        {
            error = "use /intercom message select clear (no range)";
            return false;
        }

        if (tail.Contains('['))
            return TryParseBracketSegments(tail, out segments, out error);

        return TryParseContiguous(tail, out segments, out error);
    }

    public static string Apply(int feedCount, int start, int end, out Selection selection) =>
        ApplySegments(feedCount, [new Range(start, end)], out selection);

    public static string ApplySegments(int feedCount, IReadOnlyList<Range> segments, out Selection selection)
    {
        selection = Empty;
        if (feedCount <= 0)
            return "no messages in feed";
        if (segments.Count == 0)
            return "need at least one segment [n] or [a;b]";

        var last = segments[^1];
        foreach (var segment in segments)
        {
            if (segment.Start < 1 || segment.End < 1)
                return "ordinal must be ≥ 1";
            if (segment.End < segment.Start)
                return "end ordinal < start";
            if (segment.Start > feedCount)
                return $"no message #{segment.Start} (feed {feedCount})";
            if (segment.End > feedCount)
                return $"no message #{segment.End} (feed {feedCount})";
        }

        var hi = new HashSet<int>();
        foreach (var segment in segments)
        {
            for (var o = segment.Start; o <= segment.End; o++)
                hi.Add(o);
        }

        selection = new Selection(last.End, hi);
        return "OK";
    }

    public static string ApplyOffset(int feedCount, Selection current, int delta, out Selection selection)
    {
        selection = Empty;
        if (feedCount <= 0)
            return "no messages in feed";
        if (delta == 0)
            return "offset 0";

        var from = current.ActiveOrdinal > 0
            ? current.ActiveOrdinal
            : (delta > 0 ? 0 : feedCount + 1);
        var next = from + delta;
        if (next < 1 || next > feedCount)
            return $"no message #{next} (feed {feedCount})";
        return Apply(feedCount, next, next, out selection);
    }

    public static string FormatOk(Selection selection)
    {
        if (selection.ActiveOrdinal <= 0)
            return "selection cleared";
        if (selection.Highlighted.Count <= 1)
            return $"selected #{selection.ActiveOrdinal}";

        var ordered = selection.Highlighted.OrderBy(static x => x).ToArray();
        var parts = new List<string>();
        var runStart = ordered[0];
        var runEnd = ordered[0];
        for (var i = 1; i < ordered.Length; i++)
        {
            if (ordered[i] == runEnd + 1)
            {
                runEnd = ordered[i];
                continue;
            }

            parts.Add(runStart == runEnd ? $"#{runStart}" : $"#{runStart}–#{runEnd}");
            runStart = runEnd = ordered[i];
        }

        parts.Add(runStart == runEnd ? $"#{runStart}" : $"#{runStart}–#{runEnd}");
        return $"selected {string.Join(", ", parts)} (active #{selection.ActiveOrdinal})";
    }

    static bool TryParseContiguous(string tail, out IReadOnlyList<Range> segments, out string error)
    {
        segments = [];
        error = "";
        var parts = tail.Split([' ', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "usage: /intercom message select N · N:M · [3;5] [8;15] · clear";
            return false;
        }

        if (!int.TryParse(parts[0], out var start) || start < 1)
        {
            error = "ordinal must be ≥ 1";
            return false;
        }

        if (parts.Length == 1)
        {
            segments = [new Range(start, start)];
            return true;
        }

        if (!int.TryParse(parts[1], out var end) || end < 1)
        {
            error = "ordinal must be ≥ 1";
            return false;
        }

        if (end < start)
        {
            error = "end ordinal < start";
            return false;
        }

        segments = [new Range(start, end)];
        return true;
    }

    static bool TryParseBracketSegments(string tail, out IReadOnlyList<Range> segments, out string error)
    {
        var list = new List<Range>();
        var i = 0;
        while (i < tail.Length)
        {
            while (i < tail.Length && char.IsWhiteSpace(tail[i]))
                i++;
            if (i >= tail.Length)
                break;

            if (tail[i] != '[')
            {
                error = "disjoint segments only in brackets: [3;5] [8;15]";
                segments = [];
                return false;
            }

            var close = tail.IndexOf(']', i + 1);
            if (close < 0)
            {
                error = "unclosed [";
                segments = [];
                return false;
            }

            var inner = tail[(i + 1)..close].Trim();
            if (!TryParseBracketInner(inner, out var range, out error))
            {
                segments = [];
                return false;
            }

            list.Add(range);
            i = close + 1;
        }

        if (list.Count == 0)
        {
            error = "no bracket segments";
            segments = [];
            return false;
        }

        segments = list;
        error = "";
        return true;
    }

    static bool TryParseBracketInner(string inner, out Range range, out string error)
    {
        range = default;
        error = "";
        if (inner.Length == 0)
        {
            error = "empty []";
            return false;
        }

        var parts = inner.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "inside [] expect [20] or [3;5]";
            return false;
        }

        if (!int.TryParse(parts[0], out var start) || start < 1)
        {
            error = $"bad number «{parts[0]}»";
            return false;
        }

        if (parts.Length == 1)
        {
            range = new Range(start, start);
            return true;
        }

        if (!int.TryParse(parts[1], out var end) || end < 1)
        {
            error = $"bad number «{parts[1]}»";
            return false;
        }

        if (end < start)
        {
            error = "end ordinal < start";
            return false;
        }

        range = new Range(start, end);
        return true;
    }
}
