namespace CascadeIDE.Models.AgentChat;

/// <summary>ADR 0031: пакет уточнений — единица согласования с агентом; ответы передаются структурой по id пунктов.</summary>
public sealed record ClarificationBatch(
    Guid Id,
    IReadOnlyList<ClarificationItem> Items,
    string? Title = null);
