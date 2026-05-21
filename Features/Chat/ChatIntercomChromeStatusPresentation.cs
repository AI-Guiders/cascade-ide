#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Строка состояния Intercom в Skia-toolbar (compact / Forward), вместо «хвоста» в ленте.</summary>
public static class ChatIntercomChromeStatusPresentation
{
    public const int MaxTopicChars = 28;
    public const int MaxLineChars = 24;

    public static string? FormatSubtitle(
        ChatSurfaceSnapshot snapshot,
        bool overviewMode,
        Guid detailThreadId,
        bool showTopicTabBar = false)
    {
        if (!overviewMode && showTopicTabBar)
        {
            var messageCount = ResolveMessageCount(snapshot, detailThreadId);
            return $"сообщений: {messageCount}";
        }

        if (overviewMode)
        {
            var count = snapshot.Layout.Overview.Count;
            return count == 0
                ? null
                : $"тем: {count} · {ChatThreadOverviewPresentation.CatalogFooter}";
        }

        var topic = ResolveTopicLabel(snapshot, detailThreadId);
        var line = snapshot.ProductSpine.HasContent
            ? ChatProductSpinePresentation.ResolveLineTitle(snapshot.ProductSpine)
            : null;
        var messages = ResolveMessageCount(snapshot, detailThreadId);

        var parts = new List<string>(3)
        {
            "тема: " + Truncate(topic, MaxTopicChars),
        };
        if (!string.IsNullOrWhiteSpace(line))
            parts.Add("линия: " + Truncate(line.Trim(), MaxLineChars));
        parts.Add($"сообщений: {messages}");
        return string.Join(" · ", parts);
    }

    private static string ResolveTopicLabel(ChatSurfaceSnapshot snapshot, Guid detailThreadId)
    {
        var fromOverview = snapshot.Layout.Overview.FirstOrDefault(t => t.ThreadId == detailThreadId)
            ?? snapshot.Layout.Overview.FirstOrDefault(t => t.IsActive)
            ?? snapshot.Layout.Overview.FirstOrDefault();
        if (fromOverview is not null)
            return fromOverview.Title;

        var fromLane = snapshot.Layout.Lanes.FirstOrDefault(l => l.Thread.ThreadId == detailThreadId)
            ?? snapshot.Layout.Lanes.FirstOrDefault(l => l.Thread.ThreadId == snapshot.State.ActiveThreadId)
            ?? snapshot.Layout.Lanes.FirstOrDefault();
        return fromLane?.Thread.Title ?? snapshot.State.ActiveThreadLabel;
    }

    private static int ResolveMessageCount(ChatSurfaceSnapshot snapshot, Guid detailThreadId)
    {
        var lane = snapshot.Layout.Lanes.FirstOrDefault(l => l.Thread.ThreadId == detailThreadId)
            ?? snapshot.Layout.Lanes.FirstOrDefault(l => l.Thread.ThreadId == snapshot.State.ActiveThreadId);
        if (lane is not null)
        {
            return lane.Entries.Count(e => e.Kind == ChatSurfaceEntryKind.Message);
        }

        var item = snapshot.Layout.Overview.FirstOrDefault(t => t.ThreadId == detailThreadId);
        return item?.ItemCount ?? 0;
    }

    private static string Truncate(string text, int maxChars) =>
        text.Length <= maxChars ? text : text[..maxChars] + "…";
}
