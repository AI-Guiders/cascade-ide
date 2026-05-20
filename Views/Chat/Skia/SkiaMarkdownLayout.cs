#nullable enable

namespace CascadeIDE.Views.Chat.Skia;

internal enum SkiaMarkdownStyle
{
    Plain = 0,
    Bold = 1,
    Italic = 2,
    Code = 3,
    Link = 4
}

internal readonly record struct SkiaMarkdownRun(string Text, SkiaMarkdownStyle Style);

internal readonly record struct SkiaMarkdownLine(IReadOnlyList<SkiaMarkdownRun> Runs);

/// <summary>Inline Markdown subset v1: **bold**, *italic*, `code` (ADR 0123 фаза 3).</summary>
internal static class SkiaMarkdownLayout
{
    public static IReadOnlyList<SkiaMarkdownRun> ParseInline(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)];

        var runs = new List<SkiaMarkdownRun>();
        var i = 0;
        while (i < text.Length)
        {
            if (TryBracketLink(text, i, out var bracketEnd, out var bracketInner))
            {
                AppendRun(runs, bracketInner, SkiaMarkdownStyle.Link);
                i = bracketEnd;
                continue;
            }

            if (text[i] == '[')
            {
                var close = text.IndexOf(']', i + 1);
                if (close > i)
                {
                    AppendRun(runs, text[i..(close + 1)], SkiaMarkdownStyle.Plain);
                    i = close + 1;
                    continue;
                }
            }

            if (TryDelimited(text, i, "**", out var boldEnd, out var boldInner))
            {
                AppendRun(runs, boldInner, SkiaMarkdownStyle.Bold);
                i = boldEnd;
                continue;
            }

            if (text[i] == '`' && TryDelimited(text, i, "`", out var codeEnd, out var codeInner))
            {
                AppendRun(runs, codeInner, SkiaMarkdownStyle.Code);
                i = codeEnd;
                continue;
            }

            if (TryEmphasis(text, i, '*', out var starEnd, out var starInner))
            {
                AppendRun(runs, starInner, SkiaMarkdownStyle.Italic);
                i = starEnd;
                continue;
            }

            if (TryEmphasis(text, i, '_', out var underEnd, out var underInner))
            {
                AppendRun(runs, underInner, SkiaMarkdownStyle.Italic);
                i = underEnd;
                continue;
            }

            var plainStart = i;
            while (i < text.Length
                   && !StartsWith(text, i, "**")
                   && text[i] != '`'
                   && text[i] != '*'
                   && text[i] != '_'
                   && text[i] != '[')
                i++;

            AppendRun(runs, text[plainStart..i], SkiaMarkdownStyle.Plain);
        }

        return runs.Count == 0 ? [new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain)] : runs;
    }

    public static IReadOnlyList<SkiaMarkdownLine> WrapLines(IReadOnlyList<SkiaMarkdownRun> runs, int maxChars)
    {
        maxChars = Math.Max(8, maxChars);
        var lines = new List<SkiaMarkdownLine>();
        var current = new List<SkiaMarkdownRun>();
        var currentLen = 0;

        void FlushLine()
        {
            if (current.Count == 0)
                current.Add(new SkiaMarkdownRun("", SkiaMarkdownStyle.Plain));
            lines.Add(new SkiaMarkdownLine(current.ToArray()));
            current.Clear();
            currentLen = 0;
        }

        foreach (var run in runs)
        {
            var normalized = run.Text.Replace("\r", "").Replace('\n', ' ');
            if (normalized.Length == 0)
                continue;

            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                words = [normalized];

            foreach (var word in words)
            {
                var addLen = currentLen == 0 ? word.Length : word.Length + 1;
                if (currentLen > 0 && currentLen + addLen > maxChars)
                    FlushLine();

                if (word.Length > maxChars)
                {
                    var offset = 0;
                    while (offset < word.Length)
                    {
                        if (currentLen > 0 && currentLen >= maxChars)
                            FlushLine();
                        var take = Math.Min(maxChars - currentLen, word.Length - offset);
                        if (take <= 0)
                        {
                            FlushLine();
                            take = Math.Min(maxChars, word.Length - offset);
                        }

                        AppendWord(current, ref currentLen, word.Substring(offset, take), run.Style);
                        offset += take;
                        if (currentLen >= maxChars)
                            FlushLine();
                    }

                    continue;
                }

                AppendWord(current, ref currentLen, word, run.Style);
            }
        }

        if (currentLen > 0 || lines.Count == 0)
            FlushLine();

        return lines;
    }

    public static string ToPlainText(IReadOnlyList<SkiaMarkdownLine> lines)
    {
        var sb = new System.Text.StringBuilder();
        for (var li = 0; li < lines.Count; li++)
        {
            if (li > 0)
                sb.AppendLine();
            var line = lines[li];
            for (var ri = 0; ri < line.Runs.Count; ri++)
            {
                if (ri > 0 && sb.Length > 0 && sb[^1] != '\n' && sb[^1] != ' ')
                    sb.Append(' ');
                sb.Append(line.Runs[ri].Text);
            }
        }

        return sb.ToString();
    }

    private static void AppendWord(List<SkiaMarkdownRun> line, ref int lineLen, string word, SkiaMarkdownStyle style)
    {
        if (lineLen > 0)
        {
            MergeLast(line, " ", style);
            lineLen++;
        }

        MergeLast(line, word, style);
        lineLen += word.Length;
    }

    private static void MergeLast(List<SkiaMarkdownRun> line, string text, SkiaMarkdownStyle style)
    {
        if (line.Count > 0 && line[^1].Style == style)
            line[^1] = new SkiaMarkdownRun(line[^1].Text + text, style);
        else
            line.Add(new SkiaMarkdownRun(text, style));
    }

    private static void AppendRun(List<SkiaMarkdownRun> runs, string text, SkiaMarkdownStyle style)
    {
        if (text.Length == 0)
            return;
        if (runs.Count > 0 && runs[^1].Style == style)
            runs[^1] = new SkiaMarkdownRun(runs[^1].Text + text, style);
        else
            runs.Add(new SkiaMarkdownRun(text, style));
    }

    private static bool TryDelimited(string text, int start, string delimiter, out int end, out string inner)
    {
        end = start;
        inner = "";
        if (!StartsWith(text, start, delimiter))
            return false;

        var close = text.IndexOf(delimiter, start + delimiter.Length, StringComparison.Ordinal);
        if (close < 0)
            return false;

        inner = text[(start + delimiter.Length)..close];
        end = close + delimiter.Length;
        return true;
    }

    private static bool TryEmphasis(string text, int start, char marker, out int end, out string inner)
    {
        end = start;
        inner = "";
        if (text[start] != marker)
            return false;

        if (start + 1 < text.Length && text[start + 1] == marker)
            return false;

        var close = text.IndexOf(marker, start + 1);
        if (close <= start + 1)
            return false;

        inner = text[(start + 1)..close];
        if (inner.Length == 0)
            return false;

        end = close + 1;
        return true;
    }

    private static bool TryBracketLink(string text, int start, out int end, out string span)
    {
        end = start;
        span = "";
        if (start >= text.Length || text[start] != '[')
            return false;

        var close = text.IndexOf(']', start + 1);
        if (close < 0)
            return false;

        var inner = text[(start + 1)..close];
        if (inner.Length == 0 || inner.Contains('`', StringComparison.Ordinal))
            return false;

        if (!looksLikeCodeReference(inner))
            return false;

        span = text[start..(close + 1)];
        end = close + 1;
        return true;
    }

    private static bool looksLikeCodeReference(string inner) =>
        inner.Contains(':', StringComparison.Ordinal)
        || inner.Contains(".cs", StringComparison.OrdinalIgnoreCase)
        || inner.Contains("F:", StringComparison.OrdinalIgnoreCase)
        || inner.Contains("M:", StringComparison.OrdinalIgnoreCase);

    private static bool StartsWith(string text, int index, string value) =>
        index >= 0 && index + value.Length <= text.Length
        && text.AsSpan(index, value.Length).SequenceEqual(value.AsSpan());
}
