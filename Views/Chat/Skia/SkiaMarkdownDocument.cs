#nullable enable

namespace CascadeIDE.Views.Chat.Skia;

internal enum SkiaMarkdownBlockKind
{
    Paragraph = 0,
    Heading1 = 1,
    Heading2 = 2,
    Heading3 = 3,
    Bullet = 4,
    HorizontalRule = 5,
    Blank = 6,
}

/// <summary>Одна визуальная строка документа (после wrap) с блочным контекстом.</summary>
internal readonly record struct SkiaMarkdownRow(SkiaMarkdownBlockKind Kind, IReadOnlyList<SkiaMarkdownRun> Runs);

/// <summary>Блочный Markdown subset для длинных локальных текстов (справка /help): заголовки, списки, HR, inline v1.</summary>
internal static class SkiaMarkdownDocument
{
    public static IReadOnlyList<SkiaMarkdownRow> Layout(string text, int maxChars)
    {
        maxChars = Math.Max(8, maxChars);
        if (string.IsNullOrEmpty(text))
            return [];

        var rows = new List<SkiaMarkdownRow>();
        foreach (var rawLine in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                rows.Add(new SkiaMarkdownRow(SkiaMarkdownBlockKind.Blank, []));
                continue;
            }

            if (IsHorizontalRule(line))
            {
                rows.Add(new SkiaMarkdownRow(SkiaMarkdownBlockKind.HorizontalRule, []));
                continue;
            }

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                AppendWrapped(rows, line[4..], maxChars, SkiaMarkdownBlockKind.Heading3);
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                AppendWrapped(rows, line[3..], maxChars, SkiaMarkdownBlockKind.Heading2);
                continue;
            }

            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                AppendWrapped(rows, line[2..], maxChars, SkiaMarkdownBlockKind.Heading1);
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal) || line.StartsWith("* ", StringComparison.Ordinal))
            {
                AppendBulletWrapped(rows, line[2..], maxChars);
                continue;
            }

            AppendWrapped(rows, line, maxChars, SkiaMarkdownBlockKind.Paragraph);
        }

        return rows;
    }

    private static void AppendWrapped(
        List<SkiaMarkdownRow> rows,
        string content,
        int maxChars,
        SkiaMarkdownBlockKind kind)
    {
        var wrapped = SkiaMarkdownLayout.WrapLines(SkiaMarkdownLayout.ParseInline(content), maxChars);
        foreach (var line in wrapped)
            rows.Add(new SkiaMarkdownRow(kind, line.Runs));
    }

    private static void AppendBulletWrapped(List<SkiaMarkdownRow> rows, string content, int maxChars)
    {
        var bulletMax = Math.Max(8, maxChars - 2);
        var wrapped = SkiaMarkdownLayout.WrapLines(SkiaMarkdownLayout.ParseInline(content), bulletMax);
        for (var i = 0; i < wrapped.Count; i++)
        {
            var lineRuns = new List<SkiaMarkdownRun> { new(i == 0 ? "• " : "  ", SkiaMarkdownStyle.Plain) };
            lineRuns.AddRange(wrapped[i].Runs);
            rows.Add(new SkiaMarkdownRow(SkiaMarkdownBlockKind.Bullet, lineRuns));
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
}
