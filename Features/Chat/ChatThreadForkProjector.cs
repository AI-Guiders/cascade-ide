#nullable enable
using System.Text.Json;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

internal static class ChatThreadForkProjector
{
    public static List<ChatThreadForkRecord> Project(IReadOnlyList<ChatHistoryEvent> events)
    {
        var list = new List<ChatThreadForkRecord>();
        foreach (var ev in events)
        {
            if (!string.Equals(ev.Kind, ChatHistoryEventKind.ThreadForked, StringComparison.Ordinal))
                continue;

            if (TryParse(ev.PayloadJson, out var record))
                list.Add(record);
        }

        return list;
    }

    private static bool TryParse(string payloadJson, out ChatThreadForkRecord record)
    {
        record = default!;
        ChatHistoryThreadForkedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ChatHistoryThreadForkedPayload>(payloadJson, ChatHistoryJson.Options);
        }
        catch (JsonException)
        {
            return TryParseLegacy(payloadJson, out record);
        }

        if (payload is null)
            return TryParseLegacy(payloadJson, out record);

        if (!Guid.TryParse(payload.NewThreadId, out var newId) || newId == Guid.Empty)
            return false;
        if (!Guid.TryParse(payload.PreviousThreadId, out var previousId))
            previousId = Guid.Empty;

        Guid? parentMessageId = null;
        if (!string.IsNullOrWhiteSpace(payload.ParentMessageId)
            && Guid.TryParse(payload.ParentMessageId, out var parent)
            && parent != Guid.Empty)
        {
            parentMessageId = parent;
        }

        record = new ChatThreadForkRecord(newId, previousId, parentMessageId);
        return true;
    }

    private static bool TryParseLegacy(string payloadJson, out ChatThreadForkRecord record)
    {
        record = default!;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;
            if (!TryGetGuid(root, "new_thread_id", "NewThreadId", out var newId) || newId == Guid.Empty)
                return false;
            TryGetGuid(root, "previous_thread_id", "PreviousThreadId", out var previousId);
            Guid? parentMessageId = null;
            if (TryGetGuid(root, "parent_message_id", "ParentMessageId", out var parent) && parent != Guid.Empty)
                parentMessageId = parent;
            record = new ChatThreadForkRecord(newId, previousId, parentMessageId);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetGuid(JsonElement root, string snake, string pascal, out Guid value)
    {
        value = Guid.Empty;
        if (!root.TryGetProperty(snake, out var el) && !root.TryGetProperty(pascal, out el))
            return false;
        var s = el.GetString();
        return !string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out value);
    }
}
