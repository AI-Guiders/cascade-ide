namespace CascadeIDE.Models;

/// <summary>Элемент списка моделей для UI (Cursor ACP session/new).</summary>
public sealed record CursorAcpModelPick(string ModelId, string DisplayLabel)
{
    public override string ToString() => DisplayLabel;
}
