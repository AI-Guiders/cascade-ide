#nullable enable

using System.Text.Json;
using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Services.Intercom;

/// <summary>Проекция explicit relate из event log (ADR 0137 фаза 2).</summary>
public static class IntercomMessageRangeRelatedProjector
{
    public sealed record ExplicitRelate(
        Guid ThreadId,
        int StartOrdinal,
        int EndOrdinal,
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
            payload = JsonSerializer.Deserialize<ChatHistoryMessageRangeRelatedPayload>(payloadJson, ChatHistoryJson.Options);
        }
        catch (JsonException)
        {
            return false;
        }

        if (payload is null
            || !Guid.TryParse(payload.ThreadId, out var threadId)
            || threadId == Guid.Empty
            || payload.StartOrdinal < 1
            || payload.EndOrdinal < payload.StartOrdinal
            || string.IsNullOrWhiteSpace(payload.CodeRef.File))
        {
            return false;
        }

        relate = new ExplicitRelate(
            threadId,
            payload.StartOrdinal,
            payload.EndOrdinal,
            payload.CodeRef,
            string.IsNullOrWhiteSpace(payload.Source) ? "unknown" : payload.Source);
        return true;
    }
}
