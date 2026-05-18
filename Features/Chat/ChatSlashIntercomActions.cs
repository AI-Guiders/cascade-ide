#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Локальные действия Intercom из slash (<c>kind=intercom</c>).</summary>
public static class ChatSlashIntercomActions
{
    public static bool TryExecute(
        string slashPath,
        string? argsTail,
        Guid selectedThreadId,
        Action<Guid> selectThread,
        Action<bool> setOverviewMode,
        ChatSurfaceSnapshot snapshot,
        out ChatSlashIntercomResult result,
        Action<TopicPickerPresentation>? setTopicPicker = null,
        Func<string, string>? createTopicWithTitle = null)
    {
        result = ChatSlashIntercomResult.Fail("");
        if (!IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            || route.ExecutionKind != ChatSlashCommandExecutionKind.LocalIntercom)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(route.IntercomHandlerId)
            || !ChatSlashIntercomHandlers.TryExecute(
                route.IntercomHandlerId,
                new ChatSlashIntercomHandlers.Context(
                    argsTail,
                    selectedThreadId,
                    selectThread,
                    setOverviewMode,
                    snapshot,
                    setTopicPicker,
                    createTopicWithTitle),
                out result))
        {
            result = ChatSlashIntercomResult.Fail($"Неизвестное действие: {slashPath}");
            return true;
        }

        return true;
    }

    internal static ChatSlashIntercomResult CreateTopic(string? title, Func<string, string>? createTopicWithTitle)
    {
        if (createTopicWithTitle is null)
            return ChatSlashIntercomResult.Fail("Создание тем недоступно.");

        var text = createTopicWithTitle(title ?? "");
        return IsCreateTopicFailure(text)
            ? ChatSlashIntercomResult.Fail(text)
            : ChatSlashIntercomResult.Ok(text);
    }

    internal static ChatSlashIntercomResult ShowTopicPicker(
        TopicPickerPresentation mode,
        Action<TopicPickerPresentation>? setTopicPicker,
        Action<bool> setOverviewMode,
        ChatSurfaceSnapshot snapshot)
    {
        if (setTopicPicker is null)
            return ChatSlashIntercomResult.Fail("Интерактивный список тем недоступен. Для агента: /topic list text.");

        if (snapshot.State.Threads.Count == 0)
            return ChatSlashIntercomResult.Fail(ChatThreadPresentation.EmptyTopicsHint);

        setOverviewMode(false);
        setTopicPicker(mode);
        var text = mode == TopicPickerPresentation.Tree
            ? "Дерево тем в ленте — клик по строке, чтобы открыть."
            : "Список тем в ленте — клик по строке, чтобы открыть.";
        return ChatSlashIntercomResult.Ok(text);
    }

    public static ChatSlashIntercomResult OpenTopic(
        string? query,
        Guid selectedThreadId,
        Action<Guid> selectThread,
        Action<bool> setOverviewMode,
        ChatSurfaceSnapshot snapshot)
    {
        if (!ChatSlashTopicResolver.TryResolve(query, snapshot, selectedThreadId, out var thread, out var error))
            return ChatSlashIntercomResult.Fail(error ?? "Не удалось открыть тему.");

        selectThread(thread.ThreadId);
        setOverviewMode(false);
        return ChatSlashIntercomResult.Ok($"Открыта тема: {thread.Title}");
    }

    public static ChatSlashIntercomResult OpenTopicCards(Action<bool> setOverviewMode, ChatSurfaceSnapshot snapshot)
    {
        setOverviewMode(true);
        if (!snapshot.ProductSpine.HasContent)
            return ChatSlashIntercomResult.Ok(
                "Картотека тем. Spine пуст — /spine set <текст>. Открыть тему: /topic open <имя>.");

        var title = ChatProductSpinePresentation.ResolveLineTitle(snapshot.ProductSpine);
        return ChatSlashIntercomResult.Ok(
            $"Картотека тем: spine «{title}». /topic open <имя> — открыть тему.");
    }

    private static bool IsCreateTopicFailure(string text) =>
        text.StartsWith("Укажи заголовок", StringComparison.OrdinalIgnoreCase);
}
