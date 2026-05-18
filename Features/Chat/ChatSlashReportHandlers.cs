#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Обработчики <c>kind=report</c> (поле <c>report_handler</c> в intent-catalog).</summary>
public static class ChatSlashReportHandlers
{
    public static class Ids
    {
        public const string TopicListText = "topic_list_text";
        public const string TopicTreeText = "topic_tree_text";
        public const string SpineList = "spine_list";
        public const string SpineTree = "spine_tree";
    }

    private static readonly IReadOnlyDictionary<string, Func<ChatSurfaceSnapshot, string>> Formatters =
        new Dictionary<string, Func<ChatSurfaceSnapshot, string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Ids.TopicListText] = ChatThreadPresentation.FormatTopicList,
            [Ids.TopicTreeText] = ChatThreadPresentation.FormatTopicTree,
            [Ids.SpineList] = static s => ChatSlashSessionReports.FormatSpineList(s.ProductSpine),
            [Ids.SpineTree] = static s => ChatSlashSessionReports.FormatSpineTree(s.ProductSpine),
        };

    public static bool IsKnown(string handlerId) => Formatters.ContainsKey(handlerId);

    public static bool TryFormat(string handlerId, ChatSurfaceSnapshot snapshot, out string text)
    {
        if (Formatters.TryGetValue(handlerId, out var format))
        {
            text = format(snapshot);
            return true;
        }

        text = "";
        return false;
    }
}
