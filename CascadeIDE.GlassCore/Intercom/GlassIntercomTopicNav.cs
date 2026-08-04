#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>
/// Keyboard IOP: next/prev topic card in Intercom overview (Glass <c>c:atn</c>/<c>c:atp</c>).
/// Null/empty current = All filter — next picks first topic; prev picks last.
/// </summary>
public static class GlassIntercomTopicNav
{
    public static string? Next(string? currentId, IReadOnlyList<string> topicIds)
    {
        if (topicIds is not { Count: > 0 })
            return currentId;

        if (string.IsNullOrWhiteSpace(currentId))
            return topicIds[0];

        for (var i = 0; i < topicIds.Count; i++)
        {
            if (!string.Equals(topicIds[i], currentId, StringComparison.OrdinalIgnoreCase))
                continue;
            return i + 1 < topicIds.Count ? topicIds[i + 1] : topicIds[i];
        }

        return topicIds[0];
    }

    public static string? Prev(string? currentId, IReadOnlyList<string> topicIds)
    {
        if (topicIds is not { Count: > 0 })
            return currentId;

        if (string.IsNullOrWhiteSpace(currentId))
            return topicIds[^1];

        for (var i = 0; i < topicIds.Count; i++)
        {
            if (!string.Equals(topicIds[i], currentId, StringComparison.OrdinalIgnoreCase))
                continue;
            return i > 0 ? topicIds[i - 1] : topicIds[i];
        }

        return topicIds[^1];
    }
}
