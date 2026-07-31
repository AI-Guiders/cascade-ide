#nullable enable

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// WPF façade — maps journal <see cref="GlassIntercomJournal.Entry"/> onto GlassCore cluster.
/// </summary>
internal static class GlassIntercomTopics
{
    public static readonly TimeSpan DefaultGap = CascadeIDE.Intercom.GlassIntercomTopics.DefaultGap;

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
        var stamps = entries
            .Select(e => new CascadeIDE.Intercom.GlassIntercomTopics.Stamp(e.Id, e.Body, e.StampedUtc))
            .ToList();
        return CascadeIDE.Intercom.GlassIntercomTopics.Cluster(stamps, gap)
            .Select(t => new Topic(t.Id, t.Title, t.StartUtc, t.EndUtc, t.Count, t.EntryIds))
            .ToList();
    }
}
