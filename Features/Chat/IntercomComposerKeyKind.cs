#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Нормализованные клавиши composer / CCL (Skia surface → VM).</summary>
public enum IntercomComposerKeyKind
{
    Tab,
    SlashUp,
    SlashDown,
    Escape,
    Enter,
    CommitSlashSuggestion,
    InsertNewLine,
    Backspace,
    DeleteForward,
    MoveCaretLeft,
    MoveCaretRight,
}
