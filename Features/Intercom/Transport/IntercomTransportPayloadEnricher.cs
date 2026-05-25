using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Добавляет wire extensions к payload перед POST (сохраняет локальный JSON без изменений).</summary>
internal static class IntercomTransportPayloadEnricher
{
    public static JsonElement EnrichForWire(string localKind, JsonElement payload, string? operatorMemberId = null)
    {
        if (!string.IsNullOrWhiteSpace(operatorMemberId))
        {
            var withOperator = JsonNode.Parse(payload.GetRawText()) as JsonObject ?? new JsonObject();
            withOperator["operator_member_id"] = operatorMemberId.Trim();
            payload = JsonSerializer.SerializeToElement(withOperator, IntercomTransportJson.Web);
        }

        if (!string.Equals(localKind, ChatHistoryEventKind.MessageRangeRelated, StringComparison.Ordinal))
            return payload;

        ChatHistoryMessageRangeRelatedPayload? range;
        try
        {
            range = payload.Deserialize<ChatHistoryMessageRangeRelatedPayload>(IntercomTransportJson.Web);
        }
        catch (JsonException)
        {
            return payload;
        }

        if (range is null)
            return payload;

        var root = JsonNode.Parse(payload.GetRawText()) as JsonObject ?? new JsonObject();
        var relates = root["relates_to"] as JsonArray ?? new JsonArray();
        foreach (var link in IntercomRelatesToWire.FromMessageRangeRelated(range))
        {
            var linkJson = JsonSerializer.SerializeToNode(link, IntercomTransportJson.Web);
            if (linkJson is not null)
                relates.Add(linkJson);
        }

        if (relates.Count > 0)
            root["relates_to"] = relates;

        if (IntercomMessageRangeRelatedSupport.IsDisjoint(range)
            && range.OrdinalSegments is { Count: > 0 } segments)
        {
            root["ordinal_segments"] = JsonSerializer.SerializeToNode(segments, IntercomTransportJson.Web);
        }

        return JsonSerializer.SerializeToElement(root, IntercomTransportJson.Web);
    }
}
