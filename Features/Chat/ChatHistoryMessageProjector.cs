using System.Text.Json;
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Детерминированная проекция списка сообщений из append-only событий (ADR 0045).
/// </summary>
internal static class ChatHistoryMessageProjector
{
    public sealed record Row(Guid MessageId, string Role, string Content, Guid ThreadId, Guid? ParentMessageId);

    public static List<Row> Project(IReadOnlyList<ChatHistoryEvent> events, Guid defaultThreadId)
    {
        var rows = new List<Row>();
        var indexById = new Dictionary<Guid, int>();

        foreach (var ev in events)
        {
            if (string.Equals(ev.Kind, ChatHistoryEventKind.MessageAdded, StringComparison.Ordinal)
                || string.Equals(ev.Kind, ChatHistoryEventKind.MessageCompleted, StringComparison.Ordinal))
            {
                if (!TryParseMessagePayload(ev.PayloadJson, defaultThreadId, out var row))
                    continue;
                Upsert(rows, indexById, row);
            }
            else if (string.Equals(ev.Kind, ChatHistoryEventKind.MessageEdited, StringComparison.Ordinal))
            {
                if (!TryParseMessageEdited(ev.PayloadJson, out var messageId, out var newContent))
                    continue;
                if (indexById.TryGetValue(messageId, out var idx))
                    rows[idx] = rows[idx] with { Content = newContent };
            }
        }

        return rows;
    }

    private static void Upsert(
        List<Row> rows,
        Dictionary<Guid, int> indexById,
        Row row)
    {
        if (indexById.TryGetValue(row.MessageId, out var idx))
        {
            rows[idx] = row;
            return;
        }

        indexById[row.MessageId] = rows.Count;
        rows.Add(row);
    }

    private static bool TryParseMessagePayload(string payloadJson, Guid defaultThreadId, out Row row)
    {
        row = default!;
        ChatHistoryMessagePayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ChatHistoryMessagePayload>(payloadJson, ChatHistoryJson.Options);
        }
        catch (JsonException)
        {
            return TryParseMessagePayloadLegacy(payloadJson, defaultThreadId, out row);
        }

        if (payload is null)
            return TryParseMessagePayloadLegacy(payloadJson, defaultThreadId, out row);

        if (!Guid.TryParse(payload.MessageId, out var messageId))
            messageId = Guid.NewGuid();

        var threadId = Guid.TryParse(payload.ThreadId, out var tid) && tid != Guid.Empty
            ? tid
            : defaultThreadId;

        Guid? parentMessageId = null;
        if (!string.IsNullOrWhiteSpace(payload.ParentMessageId)
            && Guid.TryParse(payload.ParentMessageId, out var parent)
            && parent != Guid.Empty)
        {
            parentMessageId = parent;
        }

        row = new Row(
            messageId,
            string.IsNullOrWhiteSpace(payload.Role) ? "assistant" : payload.Role,
            payload.Content ?? "",
            threadId,
            parentMessageId);
        return true;
    }

    private static bool TryParseMessageEdited(string payloadJson, out Guid messageId, out string newContent)
    {
        messageId = Guid.Empty;
        newContent = "";

        ChatHistoryMessageEditedPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<ChatHistoryMessageEditedPayload>(payloadJson, ChatHistoryJson.Options);
        }
        catch (JsonException)
        {
            return TryParseMessageEditedLegacy(payloadJson, out messageId, out newContent);
        }

        if (payload is null)
            return TryParseMessageEditedLegacy(payloadJson, out messageId, out newContent);

        if (!Guid.TryParse(payload.MessageId, out messageId) || messageId == Guid.Empty)
            return false;

        newContent = payload.NewContent ?? "";
        return true;
    }

    /// <summary>Чтение старых NDJSON до typed payload (ручной разбор).</summary>
    private static bool TryParseMessageEditedLegacy(string payloadJson, out Guid messageId, out string newContent)
    {
        messageId = Guid.Empty;
        newContent = "";
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("message_id", out var p) || root.TryGetProperty("MessageId", out p))
        {
            var s = p.GetString();
            if (string.IsNullOrWhiteSpace(s) || !Guid.TryParse(s, out messageId))
                return false;
        }
        else
        {
            return false;
        }

        if (root.TryGetProperty("new_content", out var nc))
            newContent = nc.GetString() ?? "";
        else if (root.TryGetProperty("NewContent", out var nc2))
            newContent = nc2.GetString() ?? "";

        return messageId != Guid.Empty;
    }

    /// <summary>NDJSON до typed payload (<c>Dictionary</c>-эра и PascalCase).</summary>
    private static bool TryParseMessagePayloadLegacy(string payloadJson, Guid defaultThreadId, out Row row)
    {
        row = default!;
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            var root = doc.RootElement;

            if (!TryGetGuidProperty(root, "message_id", "MessageId", out var messageId))
                messageId = Guid.NewGuid();

            var role = TryGetStringProperty(root, "role", "Role") ?? "assistant";
            var content = TryGetStringProperty(root, "content", "Content") ?? "";

            var threadId = defaultThreadId;
            if (TryGetGuidProperty(root, "thread_id", "ThreadId", out var tid) && tid != Guid.Empty)
                threadId = tid;

            Guid? parentMessageId = null;
            if (TryGetGuidProperty(root, "parent_message_id", "ParentMessageId", out var parent) && parent != Guid.Empty)
                parentMessageId = parent;

            row = new Row(messageId, role, content, threadId, parentMessageId);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryGetGuidProperty(
        JsonElement root,
        string snakeName,
        string pascalName,
        out Guid value)
    {
        value = Guid.Empty;
        if (!root.TryGetProperty(snakeName, out var el) && !root.TryGetProperty(pascalName, out el))
            return false;

        var s = el.GetString();
        return !string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out value);
    }

    private static string? TryGetStringProperty(JsonElement root, string snakeName, string pascalName)
    {
        if (root.TryGetProperty(snakeName, out var el) || root.TryGetProperty(pascalName, out el))
            return el.GetString();
        return null;
    }
}
