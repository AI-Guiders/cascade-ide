#nullable enable
// Adapted from AvaloniaUI/AvaloniaEdit AvaloniaEdit.TextMate (MIT) for WPF AvalonEdit.
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using TextMateSharp.Grammars;
using TextMateSharp.Model;

namespace CDP.GlassCockpit.Windows.TextMate;

sealed class TextEditorModel : AbstractLineList, IDisposable
{
    readonly TextDocument _document;
    readonly TextView _textView;
    readonly DocumentSnapshot _documentSnapshot;
    readonly Action<Exception>? _exceptionHandler;
    InvalidLineRange? _invalidRange;
    bool _isDisposed;

    public TextEditorModel(TextView textView, TextDocument document, Action<Exception>? exceptionHandler)
    {
        _textView = textView;
        _document = document;
        _exceptionHandler = exceptionHandler;
        _documentSnapshot = new DocumentSnapshot(_document);

        for (var i = 0; i < _document.LineCount; i++)
            AddLine(i);

        _document.Changing += DocumentOnChanging;
        _document.Changed += DocumentOnChanged;
        _document.UpdateFinished += DocumentOnUpdateFinished;
        _textView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
    }

    public override void Dispose()
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        Volatile.Write(ref _isDisposed, true);
        _document.Changing -= DocumentOnChanging;
        _document.Changed -= DocumentOnChanged;
        _document.UpdateFinished -= DocumentOnUpdateFinished;
        _textView.ScrollOffsetChanged -= TextView_ScrollOffsetChanged;
    }

    public override void UpdateLine(int lineIndex) { }

    public void InvalidateViewPortLines()
    {
        ThrowIfDisposed();
        if (!_textView.VisualLinesValid || _textView.VisualLines.Count == 0)
            return;
        InvalidateLineRange(
            _textView.VisualLines[0].FirstDocumentLine.LineNumber - 1,
            _textView.VisualLines[^1].LastDocumentLine.LineNumber - 1);
    }

    public override int GetNumberOfLines() => _documentSnapshot.LineCount;

    public override LineText GetLineTextIncludingTerminators(int lineIndex) =>
        new(_documentSnapshot.GetLineTextIncludingTerminator(lineIndex));

    public override int GetLineLength(int lineIndex) =>
        _documentSnapshot.GetLineLength(lineIndex);

    void TextView_ScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        TokenizeViewPort();
    }

    void DocumentOnChanging(object? sender, DocumentChangeEventArgs e)
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        try
        {
            if (e.RemovalLength <= 0)
                return;
            var startLine = _document.GetLineByOffset(e.Offset).LineNumber - 1;
            var endLine = _document.GetLineByOffset(e.Offset + e.RemovalLength).LineNumber - 1;
            for (var i = endLine; i > startLine; i--)
                RemoveLine(i);
            _documentSnapshot.RemoveLines(startLine, endLine);
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(ex);
        }
    }

    void DocumentOnChanged(object? sender, DocumentChangeEventArgs e)
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        try
        {
            var startLine = _document.GetLineByOffset(e.Offset).LineNumber - 1;
            var endLine = startLine;
            if (e.InsertionLength > 0)
            {
                endLine = _document.GetLineByOffset(e.Offset + e.InsertionLength).LineNumber - 1;
                for (var i = startLine; i < endLine; i++)
                    AddLine(i);
            }

            _documentSnapshot.Update(e);
            if (startLine == 0)
            {
                SetInvalidRange(startLine, endLine);
                return;
            }

            SetInvalidRange(startLine - 1, endLine);
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(ex);
        }
    }

    void SetInvalidRange(int startLine, int endLine)
    {
        if (!_document.IsInUpdate)
        {
            InvalidateLineRange(startLine, endLine);
            return;
        }

        _invalidRange = _invalidRange?.Merge(startLine, endLine) ?? new InvalidLineRange(startLine, endLine);
    }

    void DocumentOnUpdateFinished(object? sender, EventArgs e)
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        if (_invalidRange is null)
            return;
        try
        {
            var range = _invalidRange.Value;
            var startLine = Math.Clamp(range.StartLine, 0, _documentSnapshot.LineCount - 1);
            var endLine = Math.Clamp(range.EndLine, 0, _documentSnapshot.LineCount - 1);
            InvalidateLineRange(startLine, endLine);
        }
        finally
        {
            _invalidRange = null;
        }
    }

    void TokenizeViewPort() =>
        _textView.Dispatcher.BeginInvoke(DispatcherPriority.Normal, TokenizeViewPortCore);

    void TokenizeViewPortCore()
    {
        if (Volatile.Read(ref _isDisposed))
            return;
        try
        {
            if (!_textView.VisualLinesValid || _textView.VisualLines.Count == 0)
                return;
            ForceTokenization(
                _textView.VisualLines[0].FirstDocumentLine.LineNumber - 1,
                _textView.VisualLines[^1].LastDocumentLine.LineNumber - 1);
        }
        catch (Exception ex)
        {
            _exceptionHandler?.Invoke(ex);
        }
    }

    void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _isDisposed))
            throw new ObjectDisposedException(nameof(TextEditorModel));
    }

    readonly struct InvalidLineRange
    {
        public int StartLine { get; }
        public int EndLine { get; }

        public InvalidLineRange(int startLine, int endLine)
        {
            StartLine = startLine;
            EndLine = endLine;
        }

        public InvalidLineRange Merge(int startLine, int endLine) =>
            new(Math.Min(startLine, StartLine), Math.Max(endLine, EndLine));
    }
}
