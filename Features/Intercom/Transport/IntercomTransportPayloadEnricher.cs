using System.Text.Json;
using System.Text.Json.Nodes;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Добавляет wire extensions к payload перед POST (сохраняет локальный JSON без изменений).</summary>
internal static class IntercomTransportPayloadEnricher
{
    public static JsonElement EnrichForWire(string localKind, JsonElement payload)
    {
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

        var link = IntercomRelatesToWire.FromMessageRangeRelated(range);
        var linkJson = JsonSerializer.SerializeToNode(link, IntercomTransportJson.Web);
        if (linkJson is null)
            return payload;

        var root = JsonNode.Parse(payload.GetRawText()) as JsonObject ?? new JsonObject();
        var relates = root["relates_to"] as JsonArray ?? new JsonArray();
        relates.Add(linkJson);
        root["relates_to"] = relates;

        return JsonSerializer.SerializeToElement(root, IntercomTransportJson.Web);
    }
}
