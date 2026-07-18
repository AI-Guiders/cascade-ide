#nullable enable

namespace CascadeIDE.Features.Chat;

/// <summary>Разбор тела сообщения для Skia-ленты (кодовые блоки, thinking).</summary>
public static class ChatMessageBodyPresentation
{
    public const string CollapsedThinkingPrefix = "[thinking свернут] ";

    public static bool IsCollapsedThinking(string? body) =>
        !string.IsNullOrEmpty(body)
        && body.StartsWith(CollapsedThinkingPrefix, StringComparison.Ordinal);

    public static bool CanToggleThinking(ChatMessageVisualRole role) =>
        role == ChatMessageVisualRole.Thinking;

    /// <summary>Разбить тело на prose и fenced code (```); все блоки кода в сообщении.</summary>
    public static IReadOnlyList<ChatMessageBodySegment> SplitSegments(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return [new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, "")];

        var segments = new List<ChatMessageBodySegment>();
        var index = 0;
        while (index < body.Length)
        {
            var fenceStart = body.IndexOf("```", index, StringComparison.Ordinal);
            if (fenceStart < 0)
            {
                appendProseTail(segments, body[index..]);
                break;
            }

            if (fenceStart > index)
                appendProseTail(segments, body[index..fenceStart]);

            var afterFence = fenceStart + 3;
            var lineEnd = body.IndexOf('\n', afterFence);
            if (lineEnd < 0)
            {
                appendProseTail(segments, body[fenceStart..]);
                break;
            }

            var codeStart = lineEnd + 1;
            var endFence = body.IndexOf("```", codeStart, StringComparison.Ordinal);
            if (endFence < 0)
            {
                appendProseTail(segments, body[fenceStart..]);
                break;
            }

            var code = body[codeStart..endFence].TrimEnd('\r', '\n');
            if (code.Length > 0)
                segments.Add(new ChatMessageBodySegment(ChatMessageBodySegmentKind.Code, code));

            index = endFence + 3;
        }

        return segments.Count == 0
            ? [new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, body)]
            : segments;
    }

    /// <summary>Prose с блочной разметкой (заголовки, списки) — document layout в ленте.</summary>
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
            {
                return true;
            }

            if (line.Length >= 3
                && line.All(c => c is '-' or '*' or ' ' or '_')
                && line.Any(c => c is '-' or '*'))
            {
                return true;
            }
        }

        return false;
    }

    private static void appendProseTail(List<ChatMessageBodySegment> segments, string text)
    {
        var prose = text.TrimEnd();
        if (prose.Length > 0)
            segments.Add(new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, prose));
    }
}

public enum ChatMessageBodySegmentKind
{
    Prose = 0,
    Code = 1,
}

public readonly record struct ChatMessageBodySegment(ChatMessageBodySegmentKind Kind, string Text);
