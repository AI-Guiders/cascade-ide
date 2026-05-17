namespace CascadeIDE.Models.AgentChat;

/// <summary>Лёгкие метаданные сессии чата.</summary>
public sealed record ChatSessionMetadata(
    Guid SessionId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    string? Title = null,
    int SchemaVersion = 1,
    /// <summary>Корневая ветка по умолчанию; новые сообщения привязываются к активной ветке (см. события <c>thread_forked</c>).</summary>
    Guid MainThreadId = default,
    string? ProductSpineLineTitle = null,
    string? ProductSpineCurrentFocus = null,
    /// <summary>Вехи spine, по одной на строку.</summary>
    string? ProductSpineMilestones = null,
    bool ProductSpineIncludeInAgentContext = false);
