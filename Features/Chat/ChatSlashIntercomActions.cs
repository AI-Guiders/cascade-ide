#nullable enable
using CascadeIDE.Services;

namespace CascadeIDE.Features.Chat;

/// <summary>Локальные действия Intercom из slash (<c>kind=intercom</c>).</summary>
public static class ChatSlashIntercomActions
{
    public static string? TryExecute(
        string slashPath,
        string? argsTail,
        Guid selectedThreadId,
        Action<Guid> selectThread,
        Action<bool> setOverviewMode,
        ChatSurfaceSnapshot snapshot)
    {
        if (!IntentSlashCatalog.TryGetRoute(slashPath, out var route)
            || route.ExecutionKind != ChatSlashCommandExecutionKind.LocalIntercom)
        {
            return null;
        }

        return slashPath.ToLowerInvariant() switch
        {
            "/topic open" => OpenTopic(argsTail, selectedThreadId, selectThread, setOverviewMode, snapshot),
            "/topic cards" or "/spine open" => OpenTopicCards(setOverviewMode, snapshot),
            _ => $"Неизвестное действие: {slashPath}",
        };
    }

    public static string OpenTopic(
        string? query,
        Guid selectedThreadId,
        Action<Guid> selectThread,
        Action<bool> setOverviewMode,
        ChatSurfaceSnapshot snapshot)
    {
        if (!ChatSlashTopicResolver.TryResolve(query, snapshot, selectedThreadId, out var thread, out var error))
            return error ?? "Не удалось открыть тему.";

        selectThread(thread.ThreadId);
        setOverviewMode(false);
        return $"Открыта тема: {thread.Title}";
    }

    public static string OpenTopicCards(Action<bool> setOverviewMode, ChatSurfaceSnapshot snapshot)
    {
        setOverviewMode(true);
        if (!snapshot.ProductSpine.HasContent)
            return "Картотека тем. Spine пуст — /spine set <текст>. Открыть тему: /topic open <имя>.";

        var title = ChatProductSpinePresentation.ResolveLineTitle(snapshot.ProductSpine);
        return $"Картотека тем: spine «{title}». /topic open <имя> — открыть тему.";
    }
}
