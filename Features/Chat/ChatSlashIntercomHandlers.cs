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
        public const string AttachSelection = "attach_selection";
        public const string AttachScope = "attach_scope";
        public const string AttachFile = "attach_file";
        public const string MessageSelect = "message_select";
        public const string MessageFind = "message_find";
        public const string MessageRelate = "message_relate";
    }

    public sealed record Context(
        string? ArgsTail,
        Guid SelectedThreadId,
        Action<Guid> SelectThread,
        Action<bool> SetOverviewMode,
        ChatSurfaceSnapshot Snapshot,
        Action<TopicPickerPresentation>? SetTopicPicker,
        Func<string, TopicCreateResult>? CreateTopicWithTitle,
        Func<string, string?, ChatSlashIntercomResult>? TryAttachSlash,
        Func<int, int, string>? SelectMessageByOrdinalRangeInDetailLane = null,
        Func<string?, string>? FindMessagesForCodeRef = null,
        Func<string?, string>? RelateMessageRangeToCodeRef = null);

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
            [Ids.AttachSelection] = static ctx => executeAttach(ctx, Ids.AttachSelection),
            [Ids.AttachScope] = static ctx => executeAttach(ctx, Ids.AttachScope),
            [Ids.AttachFile] = static ctx => executeAttach(ctx, Ids.AttachFile),
            [Ids.MessageSelect] = static ctx => executeMessageSelect(ctx),
            [Ids.MessageFind] = static ctx => executeMessageFind(ctx),
            [Ids.MessageRelate] = static ctx => executeMessageRelate(ctx),
        };

    private static ChatSlashIntercomResult executeAttach(Context ctx, string handlerId)
    {
        if (ctx.TryAttachSlash is null)
            return ChatSlashIntercomResult.Fail("Attach недоступен в этой сессии.");
        return ctx.TryAttachSlash(handlerId, ctx.ArgsTail);
    }

    private static ChatSlashIntercomResult executeMessageSelect(Context ctx)
    {
        if (ctx.SelectMessageByOrdinalRangeInDetailLane is null)
            return ChatSlashIntercomResult.Fail("Выбор сообщения недоступен.");

        var tail = ctx.ArgsTail?.Trim() ?? "";
        if (!ChatSlashParametricArgsBuilder.TryParseLineRangeTail(tail, out var start, out var end, out var parseError))
            return ChatSlashIntercomResult.Fail(parseError);

        var result = ctx.SelectMessageByOrdinalRangeInDetailLane(start, end);
        if (!string.Equals(result, "OK", StringComparison.Ordinal))
            return ChatSlashIntercomResult.Fail(result);

        var text = start == end
            ? $"Выбрано сообщение #{start}."
            : $"Выбран диапазон #{start}–#{end} (активно #{end}).";
        return ChatSlashIntercomResult.Ok(text);
    }

    private static ChatSlashIntercomResult executeMessageFind(Context ctx)
    {
        if (ctx.FindMessagesForCodeRef is null)
            return ChatSlashIntercomResult.Fail("Поиск сообщений по коду недоступен.");

        var result = ctx.FindMessagesForCodeRef(ctx.ArgsTail);
        return result.StartsWith("Связанные сообщения:", StringComparison.Ordinal)
            ? ChatSlashIntercomResult.Ok(result)
            : ChatSlashIntercomResult.Fail(result);
    }

    private static ChatSlashIntercomResult executeMessageRelate(Context ctx)
    {
        if (ctx.RelateMessageRangeToCodeRef is null)
            return ChatSlashIntercomResult.Fail("Relate недоступен.");

        var result = ctx.RelateMessageRangeToCodeRef(ctx.ArgsTail);
        return result.StartsWith("Связь сообщений", StringComparison.Ordinal)
            ? ChatSlashIntercomResult.Ok(result)
            : ChatSlashIntercomResult.Fail(result);
    }

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
