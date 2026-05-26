#nullable enable

using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Проекция explicit relate из event log (ADR 0137 contiguous, 0138 disjoint).</summary>
public static class IntercomMessageRangeRelatedProjector
{
    private static readonly JsonSerializerOptions PayloadJson = new(JsonSerializerDefaults.Web);

    public sealed record ExplicitRelate(
        Guid ThreadId,
        int StartOrdinal,
        int EndOrdinal,
        IReadOnlyList<ChatHistoryMessageOrdinalSegment> OrdinalSegments,
        AttachmentAnchor CodeRef,
        string Source);

    public static IReadOnlyList<ExplicitRelate> Project(IReadOnlyList<ChatHistoryEvent> events)
    {
        var list = new List<ExplicitRelate>();
        foreach (var ev in events)
        {
            if (!string.Equals(ev.Kind, ChatHistoryEventKind.MessageRangeRelated, StringComparison.Ordinal))
                continue;

            if (!TryParsePayload(ev.PayloadJson, out var relate))
                continue;

            list.Add(relate);
        }

        return list;
    }

    public static IReadOnlyList<ExplicitRelate> ForThread(
        IReadOnlyList<ExplicitRelate> all,
        Guid threadId) =>
        all.Where(r => r.ThreadId == threadId).ToList();

    private static bool TryParsePayload(string payloadJson, out ExplicitRelate relate)
    {
        relate = default!;
        ChatHistoryMessageRangeRelatedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ChatHistoryMessageRangeRelatedPayload>(payloadJson, PayloadJson);
        }
        catch (JsonException)
        {
            return false;
        }

        if (payload is null
            || !Guid.TryParse(payload.ThreadId, out var threadId)
            || threadId == Guid.Empty
            || string.IsNullOrWhiteSpace(payload.CodeRef.File))
        {
            return false;
        }

        var segments = IntercomMessageRangeRelatedSupport.ResolveSegments(payload);
        foreach (var segment in segments)
        {
            if (segment.StartOrdinal < 1 || segment.EndOrdinal < segment.StartOrdinal)
                return false;
        }

        relate = new ExplicitRelate(
            threadId,
            payload.StartOrdinal,
            payload.EndOrdinal,
            segments,
            payload.CodeRef,
            string.IsNullOrWhiteSpace(payload.Source) ? "unknown" : payload.Source);
        return true;
    }
}
