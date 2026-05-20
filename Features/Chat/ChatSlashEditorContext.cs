#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Контекст редактора для параметрических слэш-команд (диапазон строк).</summary>
public readonly record struct ChatSlashEditorContext(
    string? CurrentFilePath,
    string? EditorText,
    int? SelectionStart = null,
    int? SelectionLength = null,
    int? CaretOffset = null);
