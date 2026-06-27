namespace CascadeIDE.Features.Editor.Application.Monaco;

/// <summary>Line/column ↔ offset mapping for <see cref="EditorNavigationService"/> (testable).</summary>
public static class EditorNavigationLineMapping
{
    public static (int start, int length) SelectionOffsetsFromLines(
        string text,
        int startLine,
        int startColumn,
        int endLine,
        int? endColumn)
    {
        var start = OffsetFromLineColumn(text, startLine, startColumn);
        var endCol = endColumn ?? int.MaxValue;
        var end = OffsetFromLineColumn(text, endLine, endCol, endOfLineIfOverflow: true);
        return (start, Math.Max(0, end - start));
    }

    public static int OffsetFromLineColumn(
        string text,
        int lineOneBased,
        int columnOneBased,
        bool endOfLineIfOverflow = false)
    {
        if (string.IsNullOrEmpty(text) || lineOneBased < 1)
            return 0;

        var lineStart = 0;
        var line = 1;
        for (var i = 0; i < text.Length && line < lineOneBased; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        if (line != lineOneBased)
            return text.Length;

        var lineEnd = text.IndexOf('\n', lineStart);
        if (lineEnd < 0)
            lineEnd = text.Length;
        var lineLen = lineEnd - lineStart;
        var col = Math.Max(1, columnOneBased);
        if (endOfLineIfOverflow && col > lineLen + 1)
            return lineEnd;
        var offset = lineStart + Math.Min(col - 1, lineLen);
        return Math.Min(offset, text.Length);
    }
}
