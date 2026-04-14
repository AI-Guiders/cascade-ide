namespace CascadeIDE.Models.AgentChat;

/// <summary>Лёгкие метаданные сессии чата.</summary>
public sealed record ChatSessionMetadata(
    Guid SessionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? Title = null,
    int SchemaVersion = 1);
