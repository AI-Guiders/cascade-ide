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

    /// <summary>Разбить тело на prose и fenced code (```), v1 — первый блок кода.</summary>
    public static IReadOnlyList<ChatMessageBodySegment> SplitSegments(string? body)
    {
        if (string.IsNullOrEmpty(body))
            return [new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, "")];

        var start = body.IndexOf("```", StringComparison.Ordinal);
        if (start < 0)
            return [new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, body)];

        var segments = new List<ChatMessageBodySegment>();
        if (start > 0)
            segments.Add(new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, body[..start].TrimEnd()));

        var afterFence = start + 3;
        var lineEnd = body.IndexOf('\n', afterFence);
        if (lineEnd < 0)
            lineEnd = body.Length;
        var codeStart = lineEnd + (lineEnd < body.Length ? 1 : 0);
        var endFence = body.IndexOf("```", codeStart, StringComparison.Ordinal);
        if (endFence < 0)
        {
            segments.Add(new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, body[start..]));
            return segments;
        }

        var code = body[codeStart..endFence].TrimEnd();
        if (code.Length > 0)
            segments.Add(new ChatMessageBodySegment(ChatMessageBodySegmentKind.Code, code));

        var tail = body[(endFence + 3)..].TrimStart();
        if (tail.Length > 0)
            segments.Add(new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, tail));

        return segments.Count == 0
            ? [new ChatMessageBodySegment(ChatMessageBodySegmentKind.Prose, body)]
            : segments;
    }
}

public enum ChatMessageBodySegmentKind
{
    Prose = 0,
    Code = 1,
}

public readonly record struct ChatMessageBodySegment(ChatMessageBodySegmentKind Kind, string Text);
