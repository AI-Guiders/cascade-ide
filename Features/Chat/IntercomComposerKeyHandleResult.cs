#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Результат обработки клавиши composer/CCL в VM (P2).</summary>
public readonly record struct IntercomComposerKeyHandleResult(
    bool Handled,
    bool SyncCommandLineFromViewModel,
    bool SyncComposerCaretFromViewModel,
    bool RunCockpitCommit,
    bool RunSendChat);
