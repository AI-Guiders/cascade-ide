using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Маппинг локальных событий ↔ wire <c>relates_to</c> (ADR 0137, 0146).</summary>
internal static class IntercomRelatesToWire
{
    public static IntercomRelatesToLink FromMessageRangeRelated(ChatHistoryMessageRangeRelatedPayload payload) =>
        new(
            TargetKind: "code",
            Relation: "documents",
            CodeRef: payload.CodeRef,
            OrdinalRange: new IntercomOrdinalRange(payload.StartOrdinal, payload.EndOrdinal),
            Confidence: MapConfidence(payload.Source));

    public static string MapConfidence(string source) =>
        source switch
        {
            "slash" => "explicit",
            "explicit" => "explicit",
            "inferred" => "inferred",
            "agent" => "agent",
            _ => "explicit",
        };
}
