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
        public const string TopicRename = "topic_rename";
        public const string AttachSelection = "attach_selection";
        public const string AttachScope = "attach_scope";
        public const string AttachFile = "attach_file";
        public const string MessageSelect = "message_select";
        public const string MessageSelectClear = "message_select_clear";
        public const string MessageFind = "message_find";
        public const string MessageRelate = "message_relate";
        public const string MessageAnchorsList = "message_anchors_list";
        public const string AnchorPeek = "anchor_peek";
        public const string ServerStatus = "server_status";
        public const string ServerStart = "server_start";
        public const string ServerStop = "server_stop";
        public const string TeamMembers = "team_members";
        public const string TeamInvite = "team_invite";
        public const string TeamSeedProject = "team_seed_project";
        public const string AgentList = "agent_list";
        public const string AgentProvision = "agent_provision";
        public const string AgentSelect = "agent_select";
    }

    public sealed record Context(
        string? ArgsTail,
        Guid SelectedThreadId,
        Action<Guid> SelectThread,
        Action<bool> SetOverviewMode,
        ChatSurfaceSnapshot Snapshot,
        Action<TopicPickerPresentation>? SetTopicPicker,
        Func<string, TopicCreateResult>? CreateTopicWithTitle,
        Func<Guid, string, TopicRenameResult>? RenameTopicWithTitle,
        Func<string, string?, ChatSlashIntercomResult>? TryAttachSlash,
        Func<int, int, string>? SelectMessageByOrdinalRangeInDetailLane = null,
        Func<IReadOnlyList<ParametricIntRange>, string>? SelectMessagesByOrdinalRangesInDetailLane = null,
        Func<string>? ClearMessageSelectionInDetailLane = null,
        Func<string?, string>? FindMessagesForCodeRef = null,
        Func<string?, string>? RelateMessageRangeToCodeRef = null,
        Func<string>? ListMessageAnchors = null,
        Func<string?, string>? PeekAnchorById = null,
        Func<string, string?, CancellationToken, Task<ChatSlashIntercomResult>>? RunAdmin = null);

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
            [Ids.TopicRename] = static ctx => ChatSlashIntercomActions.RenameTopic(
                ctx.SelectedThreadId,
                ctx.ArgsTail,
                ctx.RenameTopicWithTitle),
            [Ids.AttachSelection] = static ctx => executeAttach(ctx, Ids.AttachSelection),
            [Ids.AttachScope] = static ctx => executeAttach(ctx, Ids.AttachScope),
            [Ids.AttachFile] = static ctx => executeAttach(ctx, Ids.AttachFile),
            [Ids.MessageSelect] = static ctx => executeMessageSelect(ctx),
            [Ids.MessageSelectClear] = static ctx => executeMessageSelectClear(ctx),
            [Ids.MessageFind] = static ctx => executeMessageFind(ctx),
            [Ids.MessageRelate] = static ctx => executeMessageRelate(ctx),
            [Ids.MessageAnchorsList] = static ctx => executeMessageAnchorsList(ctx),
            [Ids.AnchorPeek] = static ctx => executeAnchorPeek(ctx),
            [Ids.ServerStatus] = static ctx => executeAdmin(ctx, Ids.ServerStatus),
            [Ids.ServerStart] = static ctx => executeAdmin(ctx, Ids.ServerStart),
            [Ids.ServerStop] = static ctx => executeAdmin(ctx, Ids.ServerStop),
            [Ids.TeamMembers] = static ctx => executeAdmin(ctx, Ids.TeamMembers),
            [Ids.TeamInvite] = static ctx => executeAdmin(ctx, Ids.TeamInvite),
            [Ids.TeamSeedProject] = static ctx => executeAdmin(ctx, Ids.TeamSeedProject),
            [Ids.AgentList] = static ctx => executeAdmin(ctx, Ids.AgentList),
            [Ids.AgentProvision] = static ctx => executeAdmin(ctx, Ids.AgentProvision),
            [Ids.AgentSelect] = static ctx => executeAdmin(ctx, Ids.AgentSelect),
        };

    private static ChatSlashIntercomResult executeAttach(Context ctx, string handlerId)
    {
        if (ctx.TryAttachSlash is null)
            return ChatSlashIntercomResult.Fail("Attach недоступен в этой сессии.");
        return ctx.TryAttachSlash(handlerId, ctx.ArgsTail);
    }

    private static ChatSlashIntercomResult executeMessageSelect(Context ctx)
    {
        var tail = ctx.ArgsTail?.Trim() ?? "";
        if (!ParametricSegmentListParser.TryParse(tail, out var segments, out var parseError))
            return ChatSlashIntercomResult.Fail(parseError);

        if (segments.Count > 1)
        {
            if (ctx.SelectMessagesByOrdinalRangesInDetailLane is null)
                return ChatSlashIntercomResult.Fail("Выбор сообщений (multi-range) недоступен.");

            var multiResult = ctx.SelectMessagesByOrdinalRangesInDetailLane(segments);
            if (!string.Equals(multiResult, "OK", StringComparison.Ordinal))
                return ChatSlashIntercomResult.Fail(multiResult);

            var labels = segments.Select(static s =>
                s.Start == s.End ? $"#{s.Start}" : $"#{s.Start}–#{s.End}");
            return ChatSlashIntercomResult.Ok($"Выбрано: {string.Join(", ", labels)}.");
        }

        if (ctx.SelectMessageByOrdinalRangeInDetailLane is null)
            return ChatSlashIntercomResult.Fail("Выбор сообщения недоступен.");

        var range = segments[0];
        var result = ctx.SelectMessageByOrdinalRangeInDetailLane(range.Start, range.End);
        if (!string.Equals(result, "OK", StringComparison.Ordinal))
            return ChatSlashIntercomResult.Fail(result);

        var text = range.Start == range.End
            ? $"Выбрано сообщение #{range.Start}."
            : $"Выбран диапазон #{range.Start}–#{range.End} (активно #{range.End}).";
        return ChatSlashIntercomResult.Ok(text);
    }

    private static ChatSlashIntercomResult executeMessageSelectClear(Context ctx)
    {
        if (ctx.ClearMessageSelectionInDetailLane is null)
            return ChatSlashIntercomResult.Fail("Сброс выбора сообщений недоступен.");

        var tail = ctx.ArgsTail?.Trim() ?? "";
        if (tail.Length > 0)
            return ChatSlashIntercomResult.Fail("Ожидается «/intercom message select clear» без аргументов.");

        var result = ctx.ClearMessageSelectionInDetailLane();
        return string.Equals(result, "OK", StringComparison.Ordinal)
            ? ChatSlashIntercomResult.Ok("Подсветка сообщений в ветке сброшена.")
            : ChatSlashIntercomResult.Fail(result);
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

    private static ChatSlashIntercomResult executeMessageAnchorsList(Context ctx)
    {
        if (ctx.ListMessageAnchors is null)
            return ChatSlashIntercomResult.Fail("Список якорей недоступен.");

        return ChatSlashIntercomResult.Ok(ctx.ListMessageAnchors());
    }

    private static ChatSlashIntercomResult executeAdmin(Context ctx, string handlerId)
    {
        if (ctx.RunAdmin is null)
            return ChatSlashIntercomResult.Fail("Intercom admin недоступен в этой сессии.");

        return ctx.RunAdmin(handlerId, ctx.ArgsTail, CancellationToken.None)
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();
    }

    private static ChatSlashIntercomResult executeAnchorPeek(Context ctx)
    {
        if (ctx.PeekAnchorById is null)
            return ChatSlashIntercomResult.Fail("Peek якоря недоступен.");

        var result = ctx.PeekAnchorById(ctx.ArgsTail);
        if (result.Contains("не найден", StringComparison.OrdinalIgnoreCase)
            || result.Contains("Укажи id", StringComparison.OrdinalIgnoreCase)
            || result.Contains("8 hex", StringComparison.OrdinalIgnoreCase))
        {
            return ChatSlashIntercomResult.Fail(result);
        }

        return ChatSlashIntercomResult.Ok(result);
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
