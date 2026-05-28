#nullable enable

namespace CascadeIDE.Services.Intercom;

/// <summary>Разбиение markdown на prose и fenced code (ADR 0129 §5).</summary>
internal static class MarkdownProseSegments
{
    public static IEnumerable<string> EnumerateProse(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            yield break;

        var i = 0;
        var inFence = false;
        while (i < markdown.Length)
        {
            if (IsFenceOpenAt(markdown, i))
            {
                var close = FindFenceClose(markdown, i + 3);
                if (close < 0)
                    yield break;

                i = close + 3;
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                i++;
                continue;
            }

            var nextFence = FindNextFenceOpen(markdown, i);
            var end = nextFence < 0 ? markdown.Length : nextFence;
            if (end > i)
                yield return markdown[i..end];

            i = end;
        }
    }

    private static bool IsFenceOpenAt(string text, int index)
    {
        if (index + 2 >= text.Length)
            return false;
        if (text[index] != '`' || text[index + 1] != '`' || text[index + 2] != '`')
            return false;

        if (index > 0 && text[index - 1] == '`')
            return false;

        return true;
    }

    private static int FindNextFenceOpen(string text, int start)
    {
        for (var i = start; i + 2 < text.Length; i++)
        {
            if (IsFenceOpenAt(text, i))
                return i;
        }

        return -1;
    }

    private static int FindFenceClose(string text, int afterOpen)
    {
        for (var i = afterOpen; i + 2 < text.Length; i++)
        {
            if (text[i] != '`' || text[i + 1] != '`' || text[i + 2] != '`')
                continue;

            if (i > afterOpen && text[i - 1] == '`')
                continue;

            return i;
        }

        return -1;
    }
}
