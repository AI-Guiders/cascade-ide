#nullable enable
// Adapted from AvaloniaUI/AvaloniaEdit AvaloniaEdit.TextMate (MIT) for WPF AvalonEdit.
using ICSharpCode.AvalonEdit.Document;

namespace CDP.GlassCockpit.Windows.TextMate;

internal sealed class DocumentSnapshot
{
    readonly TextDocument _document;
    readonly object _lock = new();
    LineRange[] _lineRanges;
    ITextSource _textSource;
    int _lineCount;

    public int LineCount
    {
        get { lock (_lock) { return _lineCount; } }
    }

    public DocumentSnapshot(TextDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        _lineRanges = new LineRange[document.LineCount];
        Update(null);
    }

    public void RemoveLines(int startLine, int endLine)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startLine);
        ArgumentOutOfRangeException.ThrowIfLessThan(endLine, startLine);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(endLine, _lineCount);
            var removeCount = endLine - startLine + 1;
            var shiftCount = _lineCount - (endLine + 1);
            if (shiftCount > 0)
                Array.Copy(_lineRanges, endLine + 1, _lineRanges, startLine, shiftCount);
            _lineCount -= removeCount;
            Array.Resize(ref _lineRanges, _lineCount);
        }
    }

    public string GetLineTextIncludingTerminator(int lineIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lineIndex, _lineCount);
            var lineRange = _lineRanges[lineIndex];
            return _textSource.GetText(lineRange.Offset, lineRange.TotalLength);
        }
    }

    public int GetLineLength(int lineIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineIndex);
        lock (_lock)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lineIndex, _lineCount);
            return _lineRanges[lineIndex].Length;
        }
    }

    public void Update(DocumentChangeEventArgs? e)
    {
        lock (_lock)
        {
            _lineCount = _document.Lines.Count;
            if (e?.OffsetChangeMap is not null && _lineRanges is not null && _lineCount == _lineRanges.Length)
                RecalculateOffsets(e);
            else
                RecomputeAllLineRanges(e);
            _textSource = _document.CreateSnapshot();
        }
    }

    void RecalculateOffsets(DocumentChangeEventArgs e)
    {
        var changedLine = _document.GetLineByOffset(e.Offset);
        var lineIndex = changedLine.LineNumber - 1;
        _lineRanges[lineIndex].Offset = changedLine.Offset;
        _lineRanges[lineIndex].Length = changedLine.Length;
        _lineRanges[lineIndex].TotalLength = changedLine.TotalLength;
        for (var i = lineIndex + 1; i < _lineCount; i++)
            _lineRanges[i].Offset = e.OffsetChangeMap.GetNewOffset(_lineRanges[i].Offset);
    }

    void RecomputeAllLineRanges(DocumentChangeEventArgs? e)
    {
        if (_lineRanges.Length != _lineCount)
            Array.Resize(ref _lineRanges, _lineCount);

        var currentLineIndex = e is not null
            ? _document.GetLineByOffset(e.Offset).LineNumber - 1
            : 0;
        var currentLine = _document.GetLineByNumber(currentLineIndex + 1);
        while (currentLine is not null)
        {
            _lineRanges[currentLineIndex].Offset = currentLine.Offset;
            _lineRanges[currentLineIndex].Length = currentLine.Length;
            _lineRanges[currentLineIndex].TotalLength = currentLine.TotalLength;
            currentLine = currentLine.NextLine;
            currentLineIndex++;
        }
    }

    struct LineRange
    {
        public int Offset;
        public int Length;
        public int TotalLength;
    }
}
