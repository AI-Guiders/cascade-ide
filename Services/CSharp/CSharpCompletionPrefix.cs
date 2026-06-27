namespace CascadeIDE.Services;

public static class CSharpCompletionPrefix
{
    public static string Extract(string sourceText, int line1, int column1)
    {
        if (string.IsNullOrEmpty(sourceText) || line1 < 1 || column1 < 1)
            return "";

        var lineStart = 0;
        var line = 1;
        for (var i = 0; i < sourceText.Length && line < line1; i++)
        {
            if (sourceText[i] == '\n')
            {
                line++;
                lineStart = i + 1;
            }
        }

        if (line != line1)
            return "";

        var lineEnd = sourceText.IndexOf('\n', lineStart);
        if (lineEnd < 0)
            lineEnd = sourceText.Length;

        var colIndex = Math.Min(column1 - 1, lineEnd - lineStart);
        if (colIndex < 0)
            return "";

        var end = lineStart + colIndex;
        var start = end;
        while (start > lineStart && IsIdentifierPart(sourceText[start - 1]))
            start--;

        return start < end ? sourceText[start..end] : "";
    }

    private static bool IsIdentifierPart(char c) =>
        char.IsLetterOrDigit(c) || c is '_' or '@';
}
