namespace CascadeIDE.Features.Chat;

/// <summary>UI-строки картотеки тем и overview (presentation).</summary>
public static class ChatThreadOverviewPresentation
{
    public static string FormatCatalogHint(int topicCount) =>
        topicCount == 1
            ? "1 тема · Enter (ato) — открыть"
            : $"{topicCount} тем · Enter (ato) — открыть";

    public const string CatalogFooter = "atp/atn — выбор · atb — сюда";

    public static string FormatTopicTitle(ChatThreadOverviewItem item)
    {
        var indent = item.Depth > 0 ? new string(' ', item.Depth * 2) : "";
        var prefix = item.IsMainThread ? "◆ " : "";
        return indent + prefix + item.Title;
    }

    public static string? FormatTopicBadges(ChatThreadOverviewItem item)
    {
        var badges = new List<string>();
        if (item.IsMainThread)
            badges.Add("основная");
        if (item.IsActive)
            badges.Add("активная");
        if (item.ItemCount > 0)
            badges.Add($"{item.ItemCount} в ленте");
        return badges.Count == 0 ? null : string.Join(" · ", badges);
    }

    public static string FormatSideThreadRowTitle(ChatThreadOverviewItem item)
    {
        var prefix = item.Depth > 0 ? new string('>', item.Depth) + " " : "";
        return prefix + item.Title;
    }

    public static string FormatSideThreadRowMeta(ChatThreadOverviewItem item)
    {
        var active = item.IsActive ? " · active" : "";
        var main = item.IsMainThread ? " · main" : "";
        return $"Сообщений: {item.ItemCount}{active}{main}";
    }

    public static string FormatThreadHeaderMeta(ChatThreadNode thread)
    {
        var meta = thread.IsMainThread ? "основная линия" : $"ветка depth {thread.Depth}";
        if (thread.IsActive)
            meta += " | активная";
        return meta;
    }
}
