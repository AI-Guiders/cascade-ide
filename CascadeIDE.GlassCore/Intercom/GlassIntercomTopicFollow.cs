#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// After stick-to-end send/receive: if a filtered topic no longer contains the newest
/// journal entry (e.g. quiet gap minted a new 30m cluster), jump filter to that topic.
/// All (null selection) stays All.
/// </summary>
public static class GlassIntercomTopicFollow
{
    public static string? AfterStickEnd(
        string? selectedTopicId,
        IReadOnlyList<GlassIntercomTopics.Topic> topics,
        string? newestEntryId)
    {
        if (topics.Count == 0 || string.IsNullOrWhiteSpace(newestEntryId))
            return selectedTopicId;

        if (selectedTopicId is null || selectedTopicId.Length == 0)
            return null; // All

        foreach (var t in topics)
        {
            if (!string.Equals(t.Id, selectedTopicId, StringComparison.OrdinalIgnoreCase))
                continue;
            foreach (var id in t.EntryIds)
            {
                if (string.Equals(id, newestEntryId, StringComparison.OrdinalIgnoreCase))
                    return selectedTopicId; // still home
            }

            break;
        }

        for (var i = topics.Count - 1; i >= 0; i--)
        {
            foreach (var id in topics[i].EntryIds)
            {
                if (string.Equals(id, newestEntryId, StringComparison.OrdinalIgnoreCase))
                    return topics[i].Id;
            }
        }

        return selectedTopicId;
    }

    /// <summary>1-based ordinal from <c>/topics N</c>; null if out of range.</summary>
    public static string? IdByOrdinal(IReadOnlyList<GlassIntercomTopics.Topic> topics, int ordinal1Based)
    {
        if (ordinal1Based < 1 || ordinal1Based > topics.Count)
            return null;
        return topics[ordinal1Based - 1].Id;
    }
}
