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
            if (string.Equals(ev.Kind, ChatHistoryEventKind.MessageAdded, StringComparison.Ordinal))
            {
                if (!TryParseMessagePayload(ev.PayloadJson, defaultThreadId, out var id, out var role, out var content, out var tid, out var parent))
                    continue;
                Upsert(rows, indexById, id, role, content, tid, parent);
            }
            else if (string.Equals(ev.Kind, ChatHistoryEventKind.MessageCompleted, StringComparison.Ordinal))
            {
                if (!TryParseMessagePayload(ev.PayloadJson, defaultThreadId, out var id, out var role, out var content, out var tid, out var parent))
                    continue;
                Upsert(rows, indexById, id, role, content, tid, parent);
            }
            else if (string.Equals(ev.Kind, ChatHistoryEventKind.MessageEdited, StringComparison.Ordinal))
            {
                if (!TryParseMessageEdited(ev.PayloadJson, out var id, out var newContent))
                    continue;
                if (indexById.TryGetValue(id, out var idx))
                    rows[idx] = rows[idx] with { Content = newContent };
            }
        }

        return rows;
    }

    private static void Upsert(
        List<Row> rows,
        Dictionary<Guid, int> indexById,
        Guid id,
        string role,
        string content,
        Guid threadId,
        Guid? parentMessageId)
    {
        if (indexById.TryGetValue(id, out var idx))
        {
            rows[idx] = new Row(id, role, content, threadId, parentMessageId);
            return;
        }

        indexById[id] = rows.Count;
        rows.Add(new Row(id, role, content, threadId, parentMessageId));
    }

    private static bool TryParseMessagePayload(
        string payloadJson,
        Guid defaultThreadId,
        out Guid messageId,
        out string role,
        out string content,
        out Guid threadId,
        out Guid? parentMessageId)
    {
        messageId = Guid.NewGuid();
        role = "assistant";
        content = "";
        threadId = defaultThreadId;
        parentMessageId = null;

        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;

        if (TryReadMessageId(root, out var parsed))
            messageId = parsed;

        if (root.TryGetProperty("Role", out var r))
            role = r.GetString() ?? role;
        else if (root.TryGetProperty("role", out var r2))
            role = r2.GetString() ?? role;

        if (root.TryGetProperty("Content", out var c))
            content = c.GetString() ?? "";
        else if (root.TryGetProperty("content", out var c2))
            content = c2.GetString() ?? "";

        if (TryReadGuidProp(root, "thread_id", out var tid) && tid != Guid.Empty)
            threadId = tid;
        else if (TryReadGuidProp(root, "ThreadId", out var tid2) && tid2 != Guid.Empty)
            threadId = tid2;

        if (TryReadGuidProp(root, "parent_message_id", out var p1) && p1 != Guid.Empty)
            parentMessageId = p1;
        else if (TryReadGuidProp(root, "ParentMessageId", out var p2) && p2 != Guid.Empty)
            parentMessageId = p2;

        return true;
    }

    private static bool TryReadGuidProp(JsonElement root, string name, out Guid g)
    {
        g = Guid.Empty;
        if (!root.TryGetProperty(name, out var p))
            return false;
        var s = p.GetString();
        return !string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out g);
    }

    private static bool TryParseMessageEdited(string payloadJson, out Guid messageId, out string newContent)
    {
        messageId = Guid.Empty;
        newContent = "";
        using var doc = JsonDocument.Parse(payloadJson);
        var root = doc.RootElement;
        if (!TryReadMessageId(root, out messageId))
            return false;

        if (root.TryGetProperty("new_content", out var nc))
            newContent = nc.GetString() ?? "";
        else if (root.TryGetProperty("NewContent", out var nc2))
            newContent = nc2.GetString() ?? "";

        return messageId != Guid.Empty;
    }

    private static bool TryReadMessageId(JsonElement root, out Guid id)
    {
        id = Guid.Empty;
        if (root.TryGetProperty("message_id", out var p) || root.TryGetProperty("MessageId", out p))
        {
            var s = p.GetString();
            if (!string.IsNullOrWhiteSpace(s) && Guid.TryParse(s, out id))
                return true;
        }

        return false;
    }
}
