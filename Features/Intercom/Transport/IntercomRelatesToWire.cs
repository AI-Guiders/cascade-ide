using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Маппинг локальных событий ↔ wire <c>relates_to</c> (ADR 0137, 0146).</summary>
internal static class IntercomRelatesToWire
{
    public static IReadOnlyList<IntercomRelatesToLink> FromMessageRangeRelated(
        ChatHistoryMessageRangeRelatedPayload payload)
    {
        var confidence = MapConfidence(payload.Source);
        return IntercomMessageRangeRelatedSupport.ResolveSegments(payload)
            .Select(segment => new IntercomRelatesToLink(
                TargetKind: "code",
                Relation: "documents",
                CodeRef: payload.CodeRef,
                OrdinalRange: new IntercomOrdinalRange(segment.StartOrdinal, segment.EndOrdinal),
                Confidence: confidence))
            .ToList();
    }

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
