namespace CascadeIDE.Services;

/// <summary>Состояние редактора для MCP (ide_get_editor_state).</summary>
public sealed class EditorStateDto
{
    public string? FilePath { get; init; }
    public int CaretLine { get; init; }
    public int CaretColumn { get; init; }
    public int SelectionStart { get; init; }
    public int SelectionLength { get; init; }
    public string SelectionText { get; init; } = "";
    /// <summary>Длина текста в редакторе (0 если пусто).</summary>
    public int ContentLength { get; init; }
    /// <summary>True, если в редакторе нет текста.</summary>
    public bool IsEmpty { get; init; }
    /// <summary>Первые N символов (если запрошен max_preview_chars).</summary>
    public string? ContentPreview { get; init; }
}
