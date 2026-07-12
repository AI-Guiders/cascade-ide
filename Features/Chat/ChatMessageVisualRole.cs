namespace CascadeIDE.Features.Chat;

/// <summary>Роль визуального оформления записи ленты (не доменная сущность, только presentation contract).</summary>
public enum ChatMessageVisualRole
{
    User = 0,
    Assistant = 1,
    Thinking = 2,
    Tool = 3,
    ClarificationPending = 4,
    ClarificationResolved = 5,
    SlashCommand = 6,
    SedmContext = 7,
    SedmIntent = 8,
    SedmDecision = 9,
    SedmLifecycle = 10,
}
