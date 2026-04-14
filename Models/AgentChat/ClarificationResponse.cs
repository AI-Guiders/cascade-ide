namespace CascadeIDE.Models.AgentChat;

/// <summary>ADR 0031: структурированные ответы пользователя на пакет (ключ — <see cref="ClarificationItem.Id"/>).</summary>
public sealed record ClarificationResponse(
    Guid BatchId,
    IReadOnlyDictionary<string, string> AnswersByItemId);
