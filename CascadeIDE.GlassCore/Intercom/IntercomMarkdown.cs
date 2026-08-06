#nullable enable

namespace CascadeIDE.Intercom;

/// <summary>Toolkit-agnostic Intercom Markdown subset (ADR 0129 / 0170): fences, headings, lists, ** * ` inline.</summary>
public enum IntercomMarkdownStyle
{
    Plain = 0,
    Bold = 1,
    Italic = 2,
    Code = 3,
    Link = 4,
}

public enum IntercomMarkdownBlockKind
{
    Paragraph = 0,
    Heading1 = 1,
    Heading2 = 2,
    Heading3 = 3,
    Bullet = 4,
    HorizontalRule = 5,
    Blank = 6,
}

public enum IntercomMarkdownSegmentKind
{
    Prose = 0,
    Code = 1,
}

public readonly record struct IntercomMarkdownRun(string Text, IntercomMarkdownStyle Style);

public readonly record struct IntercomMarkdownLine(IReadOnlyList<IntercomMarkdownRun> Runs);

public readonly record struct IntercomMarkdownRow(IntercomMarkdownBlockKind Kind, IReadOnlyList<IntercomMarkdownRun> Runs);

public readonly record struct IntercomMarkdownSegment(IntercomMarkdownSegmentKind Kind, string Text);

public static class IntercomMarkdown
{
    public static IReadOnlyList<IntercomMarkdownSegment> SplitSegments(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return [new IntercomMarkdownSegment(IntercomMarkdownSegmentKind.Prose, "")];

        var segments = new List<IntercomMarkdownSegment>();
        var index = 0;
        while (index < body.Length)
        {
            var fenceStart = body.IndexOf("```", index, StringComparison.Ordinal);
            if (fenceStart < 0)
            {
                AppendProseTail(segments, body[index..]);
                break;
            }

            if (fenceStart > index)
                AppendProseTail(segments, body[index..fenceStart]);

            var afterFence = fenceStart + 3;
            var lineEnd = body.IndexOf('\n', afterFence);
            if (lineEnd < 0)
            {
                AppendProseTail(segments, body[fenceStart..]);
                break;
            }

            var codeStart = lineEnd + 1;
            var endFence = body.IndexOf("```", codeStart, StringComparison.Ordinal);
            if (endFence < 0)
            {
                AppendProseTail(segments, body[fenceStart..]);
                break;
            }

            var code = body[codeStart..endFence].TrimEnd('\r', '\n');
            if (code.Length > 0)
                segments.Add(new IntercomMarkdownSegment(IntercomMarkdownSegmentKind.Code, code));

            index = endFence + 3;
        }

        return segments.Count == 0
            ? [new IntercomMarkdownSegment(IntercomMarkdownSegmentKind.Prose, body)]
            : segments;
    }

    public static bool ShouldUseDocumentLayout(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        foreach (var rawLine in body.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;

            if (line.StartsWith("### ", StringComparison.Ordinal)
                || line.StartsWith("## ", StringComparison.Ordinal)
                || line.StartsWith("# ", StringComparison.Ordinal)
                || line.StartsWith("- ", StringComparison.Ordinal)
                || line.StartsWith("* ", StringComparison.Ordinal))
                return true;

            if (line.Length >= 3
                && line.All(c => c is '-' or '*' or ' ' or '_')
                && line.Any(c => c is '-' or '*'))
                return true;
        }

        return false;
    }

    public static bool HasInlineMarkup(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;
        return text.AsSpan().IndexOfAny(['*', '`', '_', '[']) >= 0;
    }

    public static IReadOnlyList<IntercomMarkdownRun> ParseInline(string text)
    {
        if (string.IsNullOrEmpty(text))
            return [new IntercomMarkdownRun("", IntercomMarkdownStyle.Plain)];

        var runs = new List<IntercomMarkdownRun>();
        var i = 0;
        while (i < text.Length)
        {
            if (TryBracketLink(text, i, out var bracketEnd, out var bracketInner))
            {
                AppendRun(runs, bracketInner, IntercomMarkdownStyle.Link);
                i = bracketEnd;
                continue;
            }

            if (text[i] == '[')
            {
                var close = text.IndexOf(']', i + 1);
                if (close > i)
                {
                    AppendRun(runs, text[i..(close + 1)], IntercomMarkdownStyle.Plain);
                    i = close + 1;
                    continue;
                }
            }

            if (TryDelimited(text, i, "**", out var boldEnd, out var boldInner))
            {
                AppendRun(runs, boldInner, IntercomMarkdownStyle.Bold);
                i = boldEnd;
                continue;
            }

            if (text[i] == '`' && TryDelimited(text, i, "`", out var codeEnd, out var codeInner))
            {
                AppendRun(runs, codeInner, IntercomMarkdownStyle.Code);
                i = codeEnd;
                continue;
            }

            if (TryEmphasis(text, i, '*', out var starEnd, out var starInner))
            {
                AppendRun(runs, starInner, IntercomMarkdownStyle.Italic);
                i = starEnd;
                continue;
            }

            if (TryEmphasis(text, i, '_', out var underEnd, out var underInner))
            {
                AppendRun(runs, underInner, IntercomMarkdownStyle.Italic);
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

            if (i == plainStart)
            {
                AppendRun(runs, text[i..(i + 1)], IntercomMarkdownStyle.Plain);
                i++;
                continue;
            }

            AppendRun(runs, text[plainStart..i], IntercomMarkdownStyle.Plain);
        }

        return runs.Count == 0 ? [new IntercomMarkdownRun("", IntercomMarkdownStyle.Plain)] : runs;
    }

    public static IReadOnlyList<IntercomMarkdownLine> WrapLines(IReadOnlyList<IntercomMarkdownRun> runs, int maxChars)
    {
        maxChars = Math.Max(8, maxChars);
        var lines = new List<IntercomMarkdownLine>();
        var current = new List<IntercomMarkdownRun>();
        var currentLen = 0;

        void FlushLine()
        {
            if (current.Count == 0)
                current.Add(new IntercomMarkdownRun("", IntercomMarkdownStyle.Plain));
            lines.Add(new IntercomMarkdownLine(current.ToArray()));
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

    public static IReadOnlyList<IntercomMarkdownRow> LayoutDocument(string text, int maxChars = 96)
    {
        maxChars = Math.Max(8, maxChars);
        if (string.IsNullOrEmpty(text))
            return [];

        var rows = new List<IntercomMarkdownRow>();
        foreach (var rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                rows.Add(new IntercomMarkdownRow(IntercomMarkdownBlockKind.Blank, []));
                continue;
            }

            if (IsHorizontalRule(line))
            {
                rows.Add(new IntercomMarkdownRow(IntercomMarkdownBlockKind.HorizontalRule, []));
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                AppendWrapped(rows, line[4..], maxChars, IntercomMarkdownBlockKind.Heading3);
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AppendWrapped(rows, line[3..], maxChars, IntercomMarkdownBlockKind.Heading2);
                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                AppendWrapped(rows, line[2..], maxChars, IntercomMarkdownBlockKind.Heading1);
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                AppendBulletWrapped(rows, line[2..], maxChars);
                continue;
            }

            AppendWrapped(rows, line, maxChars, IntercomMarkdownBlockKind.Paragraph);
        }

        return rows;
    }

    public static string ToPlainText(IReadOnlyList<IntercomMarkdownLine> lines)
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

    private static void AppendProseTail(List<IntercomMarkdownSegment> segments, string text)
    {
        var prose = text.TrimEnd();
        if (prose.Length > 0)
            segments.Add(new IntercomMarkdownSegment(IntercomMarkdownSegmentKind.Prose, prose));
    }

    private static void AppendWrapped(
        List<IntercomMarkdownRow> rows,
        string content,
        int maxChars,
        IntercomMarkdownBlockKind kind)
    {
        var wrapped = WrapLines(ParseInline(content), maxChars);
        foreach (var line in wrapped)
            rows.Add(new IntercomMarkdownRow(kind, line.Runs));
    }

    private static void AppendBulletWrapped(List<IntercomMarkdownRow> rows, string content, int maxChars)
    {
        var bulletMax = Math.Max(8, maxChars - 2);
        var wrapped = WrapLines(ParseInline(content), bulletMax);
        for (var i = 0; i < wrapped.Count; i++)
        {
            var lineRuns = new List<IntercomMarkdownRun> { new(i == 0 ? "• " : "  ", IntercomMarkdownStyle.Plain) };
            lineRuns.AddRange(wrapped[i].Runs);
            rows.Add(new IntercomMarkdownRow(IntercomMarkdownBlockKind.Bullet, lineRuns));
        }
    }

    private static bool IsHorizontalRule(string line)
    {
        var t = line.Trim();
        if (t.Length < 3)
            return false;
        return t.All(c => c is '-' or '*' or ' ' or '_')
               && t.Any(c => c is '-' or '*');
    }

    private static void AppendWord(List<IntercomMarkdownRun> line, ref int lineLen, string word, IntercomMarkdownStyle style)
    {
        if (lineLen > 0)
        {
            MergeLast(line, " ", style);
            lineLen++;
        }

        MergeLast(line, word, style);
        lineLen += word.Length;
    }

    private static void MergeLast(List<IntercomMarkdownRun> line, string text, IntercomMarkdownStyle style)
    {
        if (line.Count > 0 && line[^1].Style == style)
            line[^1] = new IntercomMarkdownRun(line[^1].Text + text, style);
        else
            line.Add(new IntercomMarkdownRun(text, style));
    }

    private static void AppendRun(List<IntercomMarkdownRun> runs, string text, IntercomMarkdownStyle style)
    {
        if (text.Length == 0)
            return;
        if (runs.Count > 0 && runs[^1].Style == style)
            runs[^1] = new IntercomMarkdownRun(runs[^1].Text + text, style);
        else
            runs.Add(new IntercomMarkdownRun(text, style));
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

        if (!LooksLikeCodeReference(inner))
            return false;

        span = text[start..(close + 1)];
        end = close + 1;
        return true;
    }

    private static bool LooksLikeCodeReference(string inner)
    {
        if (inner.Contains("F:", StringComparison.OrdinalIgnoreCase)
            || inner.Contains("M:", StringComparison.OrdinalIgnoreCase))
            return true;

        // SSOT with attach peel — Telegram [dd.MM.yyyy HH:mm] must stay plain prose.
        return GlassAttachChipPeel.TryParseBracketInner(inner, out _);
    }

    private static bool StartsWith(string text, int index, string value) =>
        index >= 0 && index + value.Length <= text.Length
        && text.AsSpan(index, value.Length).SequenceEqual(value.AsSpan());
}
