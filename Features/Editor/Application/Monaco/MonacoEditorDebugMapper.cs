using CascadeIDE.Features.Editor.Application.Monaco;

namespace CascadeIDE.Features.Editor.Application;

/// <summary>Breakpoint / debug-line / agent-reveal decorations for Monaco host.</summary>
public static class MonacoEditorDebugMapper
{
    public static IReadOnlyList<CideEditorDecoration> ToBreakpointDecorations(
        string text,
        IReadOnlyList<int> breakpointLines)
    {
        if (breakpointLines.Count == 0)
            return Array.Empty<CideEditorDecoration>();

        var list = new List<CideEditorDecoration>(breakpointLines.Count);
        foreach (var line in breakpointLines)
        {
            if (line < 1)
                continue;
            var (offset, length) = WholeLineSpan(text, line);
            if (length <= 0)
                continue;
            list.Add(new CideEditorDecoration(
                offset,
                length,
                "cide-breakpoint-line",
                "Breakpoint",
                IsWholeLine: true,
                GlyphMarginClassName: "cide-breakpoint-glyph"));
        }

        return list;
    }

    public static IReadOnlyList<CideEditorDecoration> ToDebugLineDecoration(string text, int debugLine)
    {
        if (debugLine < 1)
            return Array.Empty<CideEditorDecoration>();

        var (offset, length) = WholeLineSpan(text, debugLine);
        if (length <= 0)
            return Array.Empty<CideEditorDecoration>();

        return
        [
            new CideEditorDecoration(
                offset,
                length,
                "cide-debug-line",
                "Current debug line",
                IsWholeLine: true,
                GlyphMarginClassName: "cide-debug-arrow"),
        ];
    }

    public static IReadOnlyList<CideEditorDecoration> ToAgentRevealDecorations(
        string text,
        int startLine,
        int endLine)
    {
        if (startLine < 1 || endLine < startLine)
            return Array.Empty<CideEditorDecoration>();

        var list = new List<CideEditorDecoration>(endLine - startLine + 1);
        for (var line = startLine; line <= endLine; line++)
        {
            var (offset, length) = WholeLineSpan(text, line);
            if (length <= 0)
                continue;
            list.Add(new CideEditorDecoration(
                offset,
                length,
                AgentRevealClassForLine(line, startLine, endLine),
                null,
                IsWholeLine: true));
        }

        return list;
    }

    private static string AgentRevealClassForLine(int line, int startLine, int endLine)
    {
        const string baseClass = "cide-agent-reveal-line";
        if (startLine == endLine)
            return $"{baseClass} cide-agent-reveal-single";
        if (line == startLine)
            return $"{baseClass} cide-agent-reveal-top";
        if (line == endLine)
            return $"{baseClass} cide-agent-reveal-bottom";
        return $"{baseClass} cide-agent-reveal-middle";
    }

    private static (int Offset, int Length) WholeLineSpan(string text, int lineOneBased)
    {
        if (string.IsNullOrEmpty(text) || lineOneBased < 1)
            return (0, 0);

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
            return (0, 0);

        var lineEnd = text.IndexOf('\n', lineStart);
        if (lineEnd < 0)
            lineEnd = text.Length;
        else
            lineEnd++;

        return (lineStart, Math.Max(1, lineEnd - lineStart));
    }
}
