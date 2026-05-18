#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Событие <c>thread_forked</c>: новая ветка и предыдущая активная (для дерева без сообщений).</summary>
public sealed record ChatThreadForkRecord(
    Guid NewThreadId,
    Guid PreviousThreadId,
    Guid? ParentMessageId);
