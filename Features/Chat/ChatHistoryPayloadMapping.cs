#nullable enable
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

internal static class ChatHistoryPayloadMapping
{
    public static ChatHistoryMessagePayload ToMessagePayload(ChatMessageViewModel message)
    {
        string? slashStatus = message.SlashCommandStatus?.ToString();
        return new ChatHistoryMessagePayload(
            message.MessageId.ToString("N"),
            message.Role,
            message.Content,
            message.ThreadId.ToString("N"),
            message.ParentMessageId?.ToString("N"),
            message.SlashCommandPath,
            string.IsNullOrWhiteSpace(message.SlashCommandArgs) ? null : message.SlashCommandArgs,
            slashStatus,
            message.Attachments.Count > 0 ? message.Attachments : null,
            message.SenderWorkspaceContext,
            message.Audience == IntercomMessageAudience.Channel ? null : message.Audience);
    }

    public static ChatHistoryMessageEditedPayload ToMessageEditedPayload(
        Guid messageId,
        string newContent,
        string? reason) =>
        new(
            messageId.ToString("N"),
            newContent,
            string.IsNullOrWhiteSpace(reason) ? "correction" : reason.Trim());

    public static ChatHistoryThreadForkedPayload ToThreadForkedPayload(
        Guid newThreadId,
        Guid previousThreadId,
        Guid? parentMessageId) =>
        new(
            newThreadId.ToString("N"),
            previousThreadId.ToString("N"),
            parentMessageId?.ToString("N"));

    public static ChatHistoryClarificationAnswerSubmittedPayload ToClarificationAnswerPayload(
        ClarificationResponse response,
        IReadOnlyDictionary<string, string> answers) =>
        new(response.BatchId.ToString("N"), answers);
}
