#nullable enable

using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Contiguous и disjoint ordinal segments для <see cref="ChatHistoryEventKind.MessageRangeRelated"/>.</summary>
public static class IntercomMessageRangeRelatedSupport
{
    public static IReadOnlyList<ChatHistoryMessageOrdinalSegment> ResolveSegments(
        ChatHistoryMessageRangeRelatedPayload payload)
    {
        if (payload.OrdinalSegments is { Count: > 0 } segments)
            return segments;

        return
        [
            new ChatHistoryMessageOrdinalSegment(payload.StartOrdinal, payload.EndOrdinal),
        ];
    }

    public static bool IsDisjoint(ChatHistoryMessageRangeRelatedPayload payload) =>
        ResolveSegments(payload).Count > 1;

    public static ChatHistoryMessageRangeRelatedPayload CreatePayload(
        string threadId,
        IReadOnlyList<ChatHistoryMessageOrdinalSegment> segments,
        AttachmentAnchor codeRef,
        string source)
    {
        if (segments.Count == 0)
            throw new ArgumentException("At least one ordinal segment is required.", nameof(segments));

        var min = segments.Min(s => s.StartOrdinal);
        var max = segments.Max(s => s.EndOrdinal);

        return new ChatHistoryMessageRangeRelatedPayload(
            threadId,
            min,
            max,
            codeRef,
            source,
            segments.Count > 1 ? segments : null);
    }

    public static string FormatOrdinalSummary(IReadOnlyList<ChatHistoryMessageOrdinalSegment> segments)
    {
        if (segments.Count == 0)
            return "";

        if (segments.Count == 1)
        {
            var s = segments[0];
            return s.StartOrdinal == s.EndOrdinal
                ? $"#{s.StartOrdinal}"
                : $"#{s.StartOrdinal}–#{s.EndOrdinal}";
        }

        return string.Join(", ", segments.Select(s =>
            s.StartOrdinal == s.EndOrdinal
                ? $"#{s.StartOrdinal}"
                : $"#{s.StartOrdinal}–#{s.EndOrdinal}"));
    }

    public static bool TryValidateSegmentsInLane(
        IReadOnlyList<ChatHistoryMessageOrdinalSegment> segments,
        int laneMessageCount,
        out string error)
    {
        error = "";
        foreach (var segment in segments)
        {
            if (segment.StartOrdinal < 1 || segment.EndOrdinal < 1)
            {
                error = "Номера сообщений должны быть ≥ 1.";
                return false;
            }

            if (segment.EndOrdinal < segment.StartOrdinal)
            {
                error = "Конец диапазона не может быть меньше начала.";
                return false;
            }

            if (segment.StartOrdinal > laneMessageCount)
            {
                error = $"Нет сообщения #{segment.StartOrdinal} (в ветке {laneMessageCount}).";
                return false;
            }

            if (segment.EndOrdinal > laneMessageCount)
            {
                error = $"Нет сообщения #{segment.EndOrdinal} (в ветке {laneMessageCount}).";
                return false;
            }
        }

        return true;
    }
}
