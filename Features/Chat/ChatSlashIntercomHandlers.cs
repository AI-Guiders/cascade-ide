#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Обработчики <c>kind=intercom</c> (поле <c>intercom_handler</c> в intent-catalog).</summary>
public static class ChatSlashIntercomHandlers
{
    public static class Ids
    {
        public const string TopicOpen = "topic_open";
        public const string TopicCards = "topic_cards";
        public const string SpineOpen = "spine_open";
        public const string TopicList = "topic_list";
        public const string TopicTree = "topic_tree";
        public const string TopicCreate = "topic_create";
    }

    public sealed record Context(
        string? ArgsTail,
        Guid SelectedThreadId,
        Action<Guid> SelectThread,
        Action<bool> SetOverviewMode,
        ChatSurfaceSnapshot Snapshot,
        Action<TopicPickerPresentation>? SetTopicPicker,
        Func<string, TopicCreateResult>? CreateTopicWithTitle);

    private delegate ChatSlashIntercomResult Handler(Context context);

    private static readonly IReadOnlyDictionary<string, Handler> Handlers =
        new Dictionary<string, Handler>(StringComparer.OrdinalIgnoreCase)
        {
            [Ids.TopicOpen] = static ctx => ChatSlashIntercomActions.OpenTopic(
                ctx.ArgsTail,
                ctx.SelectedThreadId,
                ctx.SelectThread,
                ctx.SetOverviewMode,
                ctx.Snapshot),
            [Ids.TopicCards] = static ctx => ChatSlashIntercomActions.OpenTopicCards(
                ctx.SetOverviewMode,
                ctx.Snapshot),
            [Ids.SpineOpen] = static ctx => ChatSlashIntercomActions.OpenTopicCards(
                ctx.SetOverviewMode,
                ctx.Snapshot),
            [Ids.TopicList] = static ctx => ChatSlashIntercomActions.ShowTopicPicker(
                TopicPickerPresentation.List,
                ctx.SetTopicPicker,
                ctx.SetOverviewMode,
                ctx.Snapshot),
            [Ids.TopicTree] = static ctx => ChatSlashIntercomActions.ShowTopicPicker(
                TopicPickerPresentation.Tree,
                ctx.SetTopicPicker,
                ctx.SetOverviewMode,
                ctx.Snapshot),
            [Ids.TopicCreate] = static ctx => ChatSlashIntercomActions.CreateTopic(
                ctx.ArgsTail,
                ctx.CreateTopicWithTitle),
        };

    public static bool IsKnown(string handlerId) => Handlers.ContainsKey(handlerId);

    public static bool TryExecute(string handlerId, Context context, out ChatSlashIntercomResult result)
    {
        if (Handlers.TryGetValue(handlerId, out var handler))
        {
            result = handler(context);
            return true;
        }

        result = ChatSlashIntercomResult.Fail("");
        return false;
    }
}
