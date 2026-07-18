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
            message.Audience == IntercomMessageAudience.Channel ? null : message.Audience,
            message.DeliveryMode);
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

    public static SedmContextCardMaterializedPayload ToContextCardMaterializedPayload(
        Guid worklineId,
        string anchorPath,
        string? anchorSymbol,
        string? worklineLabel,
        string? pathHint,
        string? triggerReason,
        IReadOnlyList<SedmAppliesEntryPayload>? applies = null) =>
        new(
            SchemaVersion: 1,
            WorklineId: worklineId.ToString("N"),
            Anchor: new SedmContextCardAnchorPayload(anchorPath, anchorSymbol),
            Workline: new SedmWorklineRefPayload(worklineId.ToString("N"), worklineLabel),
            Applies: applies,
            PathHint: pathHint,
            TriggerReason: triggerReason);

    public static SedmIntentCardRecordedPayload ToIntentCardRecordedPayload(
        Guid worklineId,
        SedmIntentCardBodyPayload card,
        IReadOnlyList<SedmIntentConsideredOptionPayload>? considered,
        string author = "operator",
        Guid? messageId = null) =>
        new(
            SchemaVersion: 1,
            Author: author,
            WorklineId: worklineId.ToString("N"),
            Card: card,
            MessageId: messageId?.ToString("N"),
            Considered: considered);

    public static SedmDecisionRecordedPayload ToDecisionRecordedPayload(
        Guid worklineId,
        SedmIntentCardBodyPayload card,
        IReadOnlyList<SedmIntentConsideredOptionPayload>? considered,
        IReadOnlyList<SedmDecisionFindingPayload>? findings,
        SedmDecisionBasisPayload? basis,
        string author = "agent",
        Guid? messageId = null) =>
        new(
            SchemaVersion: 1,
            Author: author,
            WorklineId: worklineId.ToString("N"),
            Card: card,
            MessageId: messageId?.ToString("N"),
            Considered: considered,
            Findings: findings,
            Basis: basis,
            Status: "active");

    public static SedmDecisionLifecyclePayload ToDecisionLifecyclePayload(
        Guid worklineId,
        Guid decisionEventId,
        string? reason) =>
        new(
            SchemaVersion: 1,
            WorklineId: worklineId.ToString("N"),
            DecisionEventId: decisionEventId.ToString("N"),
            Reason: reason);
}
