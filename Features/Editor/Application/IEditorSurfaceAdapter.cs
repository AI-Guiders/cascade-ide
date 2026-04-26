namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Порт поверхности редактора (ADR 0103); скрывает AvaloniaEdit / будущий хост.
/// </summary>
public interface IEditorSurfaceAdapter
{
    string? FilePath { get; }

    int CaretOffset { get; }

    int TextLength { get; }

    void GetSelection(out int start, out int length);
}
