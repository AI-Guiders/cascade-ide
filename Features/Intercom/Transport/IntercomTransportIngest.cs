using System.Text.Json;
using CascadeIDE.Models.AgentChat;
using IntercomWire;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Входящие transport events → локальный NDJSON (ADR 0144 §6).</summary>
internal static class IntercomTransportIngest
{
    public static bool TryMapToLocalEvent(
        IntercomTransportEventEnvelopeDto envelope,
        Guid sessionId,
        out ChatHistoryEvent? localEvent)
    {
        localEvent = null;
        if (!IntercomWireTransportEventKinds.SyncDefault.Contains(envelope.EventKind))
            return false;

        var kind = ToLocalKind(envelope.EventKind);
        if (kind is null)
            return false;

        if (!Guid.TryParse(envelope.ClientEventId, out var eventId))
            eventId = Guid.NewGuid();

        var at = DateTimeOffset.TryParse(envelope.OccurredAtUtc, out var occurred)
            ? occurred
            : DateTimeOffset.UtcNow;

        string? threadId = TryExtractThreadIdFromPayload(envelope);

        var payloadJson = envelope.Payload.GetRawText();
        if (string.Equals(envelope.EventKind, "message_range_related", StringComparison.Ordinal))
            payloadJson = IntercomTransportPayloadNormalizer.NormalizeInbound(envelope.EventKind, payloadJson);

        localEvent = new ChatHistoryEvent(
            eventId,
            sessionId,
            at,
            kind,
            payloadJson,
            ThreadId: threadId);

        return true;
    }

    private static string? ToLocalKind(string wireKind) =>
        wireKind switch
        {
            "message_added" => ChatHistoryEventKind.MessageAdded,
            "message_completed" => ChatHistoryEventKind.MessageCompleted,
            "message_edited" => ChatHistoryEventKind.MessageEdited,
            "thread_forked" => ChatHistoryEventKind.ThreadForked,
            "message_range_related" => ChatHistoryEventKind.MessageRangeRelated,
            _ => null,
        };

    private static string? TryExtractThreadIdFromPayload(IntercomTransportEventEnvelopeDto envelope)
    {
        try
        {
            if (string.Equals(envelope.EventKind, "thread_forked", StringComparison.Ordinal))
            {
                var fork = envelope.Payload.Deserialize<ChatHistoryThreadForkedPayload>(IntercomTransportJson.Web);
                return fork?.NewThreadId;
            }

            if (string.Equals(envelope.EventKind, "message_range_related", StringComparison.Ordinal))
            {
                var rel = envelope.Payload.Deserialize<ChatHistoryMessageRangeRelatedPayload>(IntercomTransportJson.Web);
                return rel?.ThreadId;
            }

            var msg = envelope.Payload.Deserialize<ChatHistoryMessagePayload>(IntercomTransportJson.Web);
            return msg?.ThreadId;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
