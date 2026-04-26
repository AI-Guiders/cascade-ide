using System.Linq;
using AvaloniaEdit;

namespace CascadeIDE.Features.Editor.Application;

/// <summary>
/// Реализация <see cref="IEditorSurfaceAdapter"/> для <see cref="TextEditor"/> (AvaloniaEdit).
/// </summary>
public sealed class AvaloniaEditSurfaceAdapter : IEditorSurfaceAdapter
{
    private readonly TextEditor _editor;
    private readonly string? _filePath;

    public AvaloniaEditSurfaceAdapter(TextEditor editor, string? filePath)
    {
        _editor = editor;
        _filePath = filePath;
    }

    public string? FilePath => _filePath;

    public int CaretOffset
    {
        get
        {
            var doc = _editor.Document;
            var o = _editor.TextArea.Caret.Offset;
            if (o < 0 || o > doc.TextLength)
                return 0;
            return o;
        }
    }

    public int TextLength => _editor.Document.TextLength;

    public void GetSelection(out int start, out int length)
    {
        var seg = _editor.TextArea.Selection.Segments.FirstOrDefault();
        if (seg is null)
        {
            start = CaretOffset;
            length = 0;
        }
        else
        {
            start = seg.StartOffset;
            length = seg.EndOffset - seg.StartOffset;
        }
    }
}
