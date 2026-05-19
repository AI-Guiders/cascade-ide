using System.Runtime.CompilerServices;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;

namespace CascadeIDE.Services;

/// <summary>
/// Transient reveal диапазона строк в <see cref="TextEditor"/> (ADR 0130).
/// Паритет по длительности с <see cref="UiAgentHighlight"/>.
/// </summary>
public static class EditorAgentRangeReveal
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(3);
    private static readonly ConditionalWeakTable<TextEditor, RevealState> s_states = new();

    public static string Show(TextEditor editor, int startLine, int endLine, TimeSpan? duration = null)
    {
        if (editor.Document is null)
            return "Editor has no document.";

        if (startLine < 1 || endLine < startLine)
            return "Invalid line range.";

        if (startLine > editor.Document.LineCount)
            return $"start_line {startLine} is beyond document line count {editor.Document.LineCount}.";

        endLine = Math.Min(endLine, editor.Document.LineCount);

        var state = s_states.GetValue(editor, static e => new RevealState(e));
        state.Show(startLine, endLine, duration ?? DefaultDuration);
        return "OK";
    }

    /// <summary>Прокрутить диапазон в видимую область, не меняя каретку и selection.</summary>
    public static void ScrollRangeIntoViewWithoutChangingSelection(TextEditor editor, int startLine, int endLine)
    {
        var textArea = editor.TextArea;
        var doc = editor.Document;
        if (doc is null || doc.LineCount == 0)
            return;

        startLine = Math.Clamp(startLine, 1, doc.LineCount);
        endLine = Math.Clamp(endLine, startLine, doc.LineCount);

        var caretOffset = textArea.Caret.Offset;
        var anchor = textArea.Selection.IsEmpty ? caretOffset : textArea.Selection.SurroundingSegment?.Offset ?? caretOffset;
        var anchorLength = textArea.Selection.IsEmpty ? 0 : textArea.Selection.SurroundingSegment?.Length ?? 0;

        var midLine = (startLine + endLine) / 2;
        var mid = doc.GetLineByNumber(midLine);
        textArea.Caret.Offset = mid.Offset;
        textArea.Caret.BringCaretToView();

        textArea.Caret.Offset = caretOffset;
        if (anchorLength > 0)
            textArea.Selection = Selection.Create(textArea, anchor, anchor + anchorLength);
        else
            textArea.ClearSelection();
    }

    private sealed class RevealState
    {
        private readonly TextEditor _editor;
        private readonly EditorAgentRangeRevealBackgroundRenderer _renderer;
        private IDisposable? _hideTimer;
        private int _startLine;
        private int _endLine;

        public RevealState(TextEditor editor)
        {
            _editor = editor;
            _renderer = new EditorAgentRangeRevealBackgroundRenderer(() =>
            {
                if (_startLine < 1 || _endLine < _startLine)
                    return null;
                return (_startLine, _endLine);
            });
            _editor.TextArea.TextView.BackgroundRenderers.Add(_renderer);
        }

        public void Show(int startLine, int endLine, TimeSpan duration)
        {
            _startLine = startLine;
            _endLine = endLine;
            ScrollRangeIntoViewWithoutChangingSelection(_editor, startLine, endLine);
            _editor.TextArea.TextView.InvalidateLayer(_renderer.Layer);

            _hideTimer?.Dispose();
            _hideTimer = DispatcherTimer.RunOnce(() =>
            {
                _startLine = 0;
                _endLine = 0;
                _editor.TextArea.TextView.InvalidateLayer(_renderer.Layer);
                _hideTimer = null;
            }, duration);
        }
    }
}
