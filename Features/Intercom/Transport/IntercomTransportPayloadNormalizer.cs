using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Нормализует входящий wire payload для локального NDJSON (disjoint из <c>relates_to[]</c>).</summary>
internal static class IntercomTransportPayloadNormalizer
{
    public static string NormalizeInbound(string wireKind, string payloadJson)
    {
        if (!string.Equals(wireKind, "message_range_related", StringComparison.Ordinal))
            return payloadJson;

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(payloadJson) as JsonObject;
        }
        catch (JsonException)
        {
            return payloadJson;
        }

        if (root is null)
            return payloadJson;

        if (root["ordinal_segments"] is JsonArray { Count: > 0 })
            return payloadJson;

        if (root["relates_to"] is not JsonArray relates || relates.Count == 0)
            return payloadJson;

        var segments = new List<ChatHistoryMessageOrdinalSegment>();
        foreach (var linkNode in relates)
        {
            if (linkNode is not JsonObject link)
                continue;

            if (link["ordinal_range"] is not JsonObject ordinalRange)
                continue;

            if (!ordinalRange.TryGetPropertyValue("start_ordinal", out var startNode)
                || !ordinalRange.TryGetPropertyValue("end_ordinal", out var endNode))
            {
                continue;
            }

            if (startNode is null || endNode is null)
                continue;

            var start = startNode.GetValue<int>();
            var end = endNode.GetValue<int>();
            if (start < 1 || end < start)
                continue;

            segments.Add(new ChatHistoryMessageOrdinalSegment(start, end));
        }

        if (segments.Count <= 1)
            return payloadJson;

        root["ordinal_segments"] = JsonSerializer.SerializeToNode(segments, IntercomTransportJson.Web);
        root["start_ordinal"] = segments.Min(s => s.StartOrdinal);
        root["end_ordinal"] = segments.Max(s => s.EndOrdinal);

        return root.ToJsonString(IntercomTransportJson.Web);
    }
}
