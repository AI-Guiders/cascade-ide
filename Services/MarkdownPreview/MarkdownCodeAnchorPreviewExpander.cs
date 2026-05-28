#nullable enable

using System.Text;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>
/// Превращает bracket code anchors в prose в markdown-ссылки для preview (ADR 0156 §2.4).
/// Схема <c>cascade-code-anchor:</c> — не http; рендерер рисует как code-link.
/// </summary>
public static class MarkdownCodeAnchorPreviewExpander
{
    public const string UriScheme = "cascade-code-anchor:";

    public static string Expand(string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return markdown;

        var sb = new StringBuilder(markdown.Length + 64);
        var i = 0;
        var inFence = false;
        while (i < markdown.Length)
        {
            if (IsFenceOpenAt(markdown, i))
            {
                var close = FindFenceClose(markdown, i + 3);
                if (close < 0)
                {
                    sb.Append(markdown.AsSpan(i));
                    break;
                }

                sb.Append(markdown.AsSpan(i, close + 3 - i));
                i = close + 3;
                inFence = !inFence;
                continue;
            }

            if (inFence)
            {
                sb.Append(markdown[i]);
                i++;
                continue;
            }

            if (markdown[i] == '['
                && BracketCodeReferenceParser.TryReadBracketSpan(markdown, i, out var closeBracket))
            {
                var inner = markdown.Substring(i + 1, closeBracket - i - 1);
                if (BracketCodeReferenceParser.TryParse(inner, out var reference, out _)
                    && !string.IsNullOrWhiteSpace(reference.File)
                    && !BracketCodeReferenceParser.IsMarkdownLinkAfter(markdown, closeBracket))
                {
                    var label = BuildLinkLabel(reference);
                    var url = UriScheme + Uri.EscapeDataString(inner.Trim());
                    sb.Append('[').Append(EscapeMarkdownLinkLabel(label)).Append("](").Append(url).Append(')');
                    i = closeBracket + 1;
                    continue;
                }
            }

            sb.Append(markdown[i]);
            i++;
        }

        return sb.ToString();
    }

    private static string BuildLinkLabel(in BracketCodeReference reference)
    {
        if (!string.IsNullOrWhiteSpace(reference.MemberKey))
        {
            var file = reference.File;
            var tail = string.IsNullOrWhiteSpace(file) ? "" : $" › {Path.GetFileName(file.Replace('\\', '/'))}";
            return reference.MemberKey.Trim() + tail;
        }

        if (!string.IsNullOrWhiteSpace(reference.File))
            return Path.GetFileName(reference.File.Replace('\\', '/'));

        return "code";
    }

    private static string EscapeMarkdownLinkLabel(string label) =>
        label.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("]", "\\]", StringComparison.Ordinal);

    private static bool IsFenceOpenAt(string text, int index)
    {
        if (index + 2 >= text.Length)
            return false;
        if (text[index] != '`' || text[index + 1] != '`' || text[index + 2] != '`')
            return false;

        return index <= 0 || text[index - 1] != '`';
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
