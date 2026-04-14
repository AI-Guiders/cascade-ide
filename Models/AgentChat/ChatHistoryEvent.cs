namespace CascadeIDE.Models.AgentChat;

/// <summary>
/// Append-only событие истории чата.
/// Payload хранится как JSON-строка для мягкой эволюции схемы.
/// </summary>
public sealed record ChatHistoryEvent(
    Guid EventId,
    Guid SessionId,
    DateTimeOffset AtUtc,
    string Kind,
    string PayloadJson,
    string? ThreadId = null,
    int SchemaVersion = 1);
