using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Правила fan-out на team transport (ADR 0144 §5, фаза 3.1).</summary>
public static class IntercomTransportPublishRules
{
    public static bool ShouldPublish(string eventKind, string payloadJson, bool syncAgentChannelMessages = true)
    {
        if (IsNeverSynced(eventKind))
            return false;

        return eventKind switch
        {
            ChatHistoryEventKind.MessageAdded or ChatHistoryEventKind.MessageCompleted
                or ChatHistoryEventKind.MessageEdited => IsChannelMessagePayload(payloadJson, syncAgentChannelMessages),
            ChatHistoryEventKind.ThreadForked or ChatHistoryEventKind.MessageRangeRelated => true,
            _ => false,
        };
    }

    public static string ToWireEventKind(string localKind) =>
        localKind switch
        {
            ChatHistoryEventKind.MessageCompleted => "message_completed",
            ChatHistoryEventKind.MessageEdited => "message_edited",
            ChatHistoryEventKind.ThreadForked => "thread_forked",
            ChatHistoryEventKind.MessageRangeRelated => "message_range_related",
            _ => "message_added",
        };

    public static string ResolveWireSenderRole(string payloadJson, string localKind)
    {
        if (localKind is ChatHistoryEventKind.ThreadForked or ChatHistoryEventKind.MessageRangeRelated)
            return "human";

        if (!TryDeserializeMessage(payloadJson, out var payload))
            return "human";

        return payload.Role.ToLowerInvariant() switch
        {
            "assistant" => "agent",
            "system" => "system",
            _ => "human",
        };
    }

    public static string? TryExtractThreadId(string eventKind, string payloadJson)
    {
        if (eventKind == ChatHistoryEventKind.ThreadForked)
        {
            try
            {
                var fork = System.Text.Json.JsonSerializer.Deserialize<ChatHistoryThreadForkedPayload>(
                    payloadJson,
                    IntercomTransportJson.Web);
                return fork?.NewThreadId;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        if (eventKind == ChatHistoryEventKind.MessageRangeRelated)
        {
            try
            {
                var rel = System.Text.Json.JsonSerializer.Deserialize<ChatHistoryMessageRangeRelatedPayload>(
                    payloadJson,
                    IntercomTransportJson.Web);
                return rel?.ThreadId;
            }
            catch (System.Text.Json.JsonException)
            {
                return null;
            }
        }

        if (!TryDeserializeMessage(payloadJson, out var msg))
            return null;

        return msg.ThreadId;
    }

    public static string TopicTitleForThread(string threadId) =>
        string.IsNullOrWhiteSpace(threadId) ? "general" : $"thread-{threadId}";

    private static bool IsNeverSynced(string eventKind) =>
        string.Equals(eventKind, ChatHistoryEventKind.MessageStreamDelta, StringComparison.Ordinal)
        || string.Equals(eventKind, ChatHistoryEventKind.ClarificationBatchOpened, StringComparison.Ordinal)
        || string.Equals(eventKind, ChatHistoryEventKind.ClarificationAnswerSubmitted, StringComparison.Ordinal);

    private static bool IsChannelMessagePayload(string payloadJson, bool syncAgentChannelMessages)
    {
        if (!TryDeserializeMessage(payloadJson, out var payload))
            return false;

        if (payload.Audience is IntercomMessageAudience.SelfOnly)
            return false;

        if (!string.IsNullOrWhiteSpace(payload.SlashCommandPath))
            return false;

        if (!syncAgentChannelMessages
            && string.Equals(payload.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool TryDeserializeMessage(string payloadJson, out ChatHistoryMessagePayload payload)
    {
        payload = default!;
        try
        {
            var p = System.Text.Json.JsonSerializer.Deserialize<ChatHistoryMessagePayload>(
                payloadJson,
                IntercomTransportJson.Web);
            if (p is null)
                return false;
            payload = p;
            return true;
        }
        catch (System.Text.Json.JsonException)
        {
            return false;
        }
    }
}
