#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Динамические подсказки для slash-маршрута (ADR 0125).</summary>
public enum SlashCompletionKind
{
    None = 0,
    WorkspaceFiles = 1,
    SessionTopics = 2,
}
