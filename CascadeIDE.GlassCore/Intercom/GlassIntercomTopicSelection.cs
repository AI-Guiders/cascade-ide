#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Keep Virtual History topic filter across journal reload / recluster.
/// Topic <see cref="GlassIntercomTopics.Topic.Id"/> is the first entry id — LoadTail can
/// age that entry out and mint a new id for the same quiet-gap bucket.
/// </summary>
public static class GlassIntercomTopicSelection
{
    /// <summary>
    /// Prefer exact <paramref name="selectedId"/>; else topic with most overlap vs
    /// <paramref name="priorEntryIds"/>. Null → All (no filter).
    /// </summary>
    public static string? Survive(
        string? selectedId,
        IReadOnlyList<GlassIntercomTopics.Topic> topics,
        IReadOnlyList<string>? priorEntryIds = null)
    {
        if (topics.Count == 0)
            return null;

        if (selectedId is { Length: > 0 })
        {
            foreach (var t in topics)
            {
                if (string.Equals(t.Id, selectedId, StringComparison.OrdinalIgnoreCase))
                    return t.Id;
            }
        }

        if (priorEntryIds is not { Count: > 0 })
            return null;

        var prior = priorEntryIds
            .Where(id => id.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (prior.Count == 0)
            return null;

        GlassIntercomTopics.Topic? best = null;
        var bestHits = 0;
        foreach (var t in topics)
        {
            var hits = 0;
            foreach (var id in t.EntryIds)
            {
                if (prior.Contains(id))
                    hits++;
            }

            if (hits > bestHits)
            {
                bestHits = hits;
                best = t;
            }
        }

        return bestHits > 0 ? best!.Id : null;
    }
}
