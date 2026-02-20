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
}
