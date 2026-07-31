#nullable enable
using System.Text;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Virtual History topic cards — cluster journal entries by quiet gap (default 30m).
/// Operator scroll/filter; not Cursor transcript summarization.
/// </summary>
internal static class GlassIntercomTopics
{
    public static readonly TimeSpan DefaultGap = TimeSpan.FromMinutes(30);

    public sealed record Topic(
        string Id,
        string Title,
        DateTimeOffset StartUtc,
        DateTimeOffset EndUtc,
        int Count,
        IReadOnlyList<string> EntryIds);

    public static IReadOnlyList<Topic> Cluster(
        IReadOnlyList<GlassIntercomJournal.Entry> entries,
        TimeSpan? gap = null)
    {
        if (entries.Count == 0)
            return [];

        var quiet = gap ?? DefaultGap;
        if (quiet <= TimeSpan.Zero)
            quiet = DefaultGap;

        var ordered = entries.OrderBy(e => e.StampedUtc).ToList();
        var topics = new List<Topic>();
        var bucket = new List<GlassIntercomJournal.Entry> { ordered[0] };

        for (var i = 1; i < ordered.Count; i++)
        {
            var prev = ordered[i - 1];
            var cur = ordered[i];
            if (cur.StampedUtc - prev.StampedUtc > quiet)
            {
                topics.Add(Build(bucket, topics.Count + 1));
                bucket = [cur];
            }
            else
            {
                bucket.Add(cur);
            }
        }

        topics.Add(Build(bucket, topics.Count + 1));
        return topics;
    }

    static Topic Build(List<GlassIntercomJournal.Entry> bucket, int ordinal)
    {
        var start = bucket[0].StampedUtc;
        var end = bucket[^1].StampedUtc;
        var firstLine = FirstLine(bucket[0].Body);
        var title = Truncate(firstLine, 42);
        if (string.IsNullOrWhiteSpace(title))
            title = $"topic {ordinal}";

        var when = start.ToLocalTime().ToString("HH:mm");
        if (end.ToLocalTime().Date == start.ToLocalTime().Date && end != start)
            when += "–" + end.ToLocalTime().ToString("HH:mm");
        else if (end.ToLocalTime().Date != start.ToLocalTime().Date)
            when = start.ToLocalTime().ToString("MM-dd HH:mm");

        var label = $"{when} · {bucket.Count} · {title}";
        var ids = bucket.Select(e => e.Id).Where(id => id.Length > 0).ToArray();
        var id = ids.Length > 0 ? ids[0] : $"t{ordinal}-{start.UtcTicks}";
        return new Topic(id, label, start, end, bucket.Count, ids);
    }

    static string FirstLine(string body)
    {
        if (string.IsNullOrEmpty(body))
            return "";
        var nl = body.IndexOf('\n');
        return nl < 0 ? body.Trim() : body[..nl].Trim();
    }

    static string Truncate(string s, int max)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= max)
            return s;
        return s[..(max - 1)].TrimEnd() + "…";
    }
}
