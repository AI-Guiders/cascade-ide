#nullable enable

namespace CascadeIDE.Features.Chat;

public sealed class SessionTopicSlashCompletionProvider : ISessionTopicSlashCompletionProvider
{
    private readonly Func<ChatSurfaceSnapshot> _getSnapshot;

    public SessionTopicSlashCompletionProvider(Func<ChatSurfaceSnapshot> getSnapshot) =>
        _getSnapshot = getSnapshot;

    public IReadOnlyList<SessionTopicSlashMatch> GetMatches(string titleOrIdPrefix, int limit)
    {
        if (limit <= 0)
            return [];

        var prefix = (titleOrIdPrefix ?? "").Trim();
        var items = _getSnapshot().Layout.Overview;
        if (items.Count == 0)
            return [];

        IEnumerable<ChatThreadOverviewItem> ranked = items;
        if (prefix.Length > 0)
        {
            ranked = items
                .Select(item => (item, Rank(prefix, item)))
                .Where(x => x.Item2 < int.MaxValue)
                .OrderBy(x => x.Item2)
                .ThenBy(x => x.item.Title, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.item);
        }
        else
        {
            ranked = items.OrderBy(i => i.IsActive ? 0 : 1).ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase);
        }

        return ranked
            .Take(limit)
            .Select(item =>
            {
                var shortId = item.ThreadId.ToString("N")[..8];
                var flags = new List<string>();
                if (item.IsMainThread)
                    flags.Add("main");
                if (item.IsActive)
                    flags.Add("active");
                var flagText = flags.Count == 0 ? "" : " · " + string.Join(", ", flags);
                return new SessionTopicSlashMatch(
                    item.Title,
                    $"{item.Title}{flagText} · {item.ItemCount} сообщ. · {shortId}");
            })
            .ToList();
    }

    private static int Rank(string prefix, ChatThreadOverviewItem item)
    {
        var p = prefix.AsSpan();
        var id = item.ThreadId.ToString("N");
        if (id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (item.Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 1;
        if (item.Title.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            return 2;
        return int.MaxValue;
    }
}
