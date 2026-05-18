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
        var threads = _getSnapshot().State.Threads;
        if (threads.Count == 0)
            return [];

        var counts = ChatThreadPresentation.MessageCountsByThread(_getSnapshot());
        return ChatThreadPresentation.RankThreadsForCompletion(threads, prefix)
            .Take(limit)
            .Select(thread =>
            {
                var shortId = thread.ThreadId.ToString("N")[..8];
                counts.TryGetValue(thread.ThreadId, out var messageCount);
                var flags = ChatThreadPresentation.FormatFlags(thread);
                var help = string.IsNullOrEmpty(flags)
                    ? $"{messageCount} сообщ."
                    : $"{flags} · {messageCount} сообщ.";
                return new SessionTopicSlashMatch(
                    shortId,
                    $"{shortId} · {thread.Title}",
                    help);
            })
            .ToList();
    }
}
