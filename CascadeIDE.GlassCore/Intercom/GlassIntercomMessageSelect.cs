#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>ADR 0136 Glass port — same contiguous ordinals as CIDE (Avalonia-free; no Skia gutter host).</summary>
public static class GlassIntercomMessageSelect
{
    public readonly record struct Selection(int ActiveOrdinal, IReadOnlySet<int> Highlighted);

    public static readonly Selection Empty = new(0, new HashSet<int>());

    public static bool IsClear(string? argsTail) =>
        string.Equals((argsTail ?? "").Trim(), "clear", StringComparison.OrdinalIgnoreCase);

    /// <summary>Parse «N», «N M», «N:M» — same contiguous tails as CIDE ParametricSegmentListParser.</summary>
    public static bool TryParseRange(string? argsTail, out int start, out int end, out string error)
    {
        start = 0;
        end = 0;
        error = "";
        var tail = (argsTail ?? "").Trim();
        if (tail.Length == 0)
        {
            error = "usage: /intercom message select N · N:M · clear";
            return false;
        }

        if (IsClear(tail))
        {
            error = "use /intercom message select clear (no range)";
            return false;
        }

        var parts = tail.Split([' ', ':'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2)
        {
            error = "usage: /intercom message select N · N:M · clear";
            return false;
        }

        if (!int.TryParse(parts[0], out start) || start < 1)
        {
            error = "ordinal must be ≥ 1";
            return false;
        }

        if (parts.Length == 1)
        {
            end = start;
            return true;
        }

        if (!int.TryParse(parts[1], out end) || end < 1)
        {
            error = "ordinal must be ≥ 1";
            return false;
        }

        if (end < start)
        {
            error = "end ordinal < start";
            return false;
        }

        return true;
    }

    public static string Apply(int feedCount, int start, int end, out Selection selection)
    {
        selection = Empty;
        if (feedCount <= 0)
            return "no messages in feed";
        if (start > feedCount)
            return $"no message #{start} (feed {feedCount})";
        if (end > feedCount)
            return $"no message #{end} (feed {feedCount})";

        var hi = new HashSet<int>();
        for (var o = start; o <= end; o++)
            hi.Add(o);
        selection = new Selection(end, hi);
        return "OK";
    }

    public static string FormatOk(Selection selection)
    {
        if (selection.ActiveOrdinal <= 0)
            return "selection cleared";
        if (selection.Highlighted.Count <= 1)
            return $"selected #{selection.ActiveOrdinal}";
        var min = selection.Highlighted.Min();
        var max = selection.Highlighted.Max();
        return $"selected #{min}–#{max} (active #{selection.ActiveOrdinal})";
    }
}
