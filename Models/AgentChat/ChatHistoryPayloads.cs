using System.Text.Json.Serialization;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Models.AgentChat;

/// <summary>Снимок сообщения для <see cref="ChatHistoryEventKind.MessageAdded"/> / MessageCompleted.</summary>
public sealed record ChatHistoryMessagePayload(
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("thread_id")] string ThreadId,
    [property: JsonPropertyName("parent_message_id")] string? ParentMessageId = null,
    [property: JsonPropertyName("slash_command_path")] string? SlashCommandPath = null,
    [property: JsonPropertyName("slash_command_args")] string? SlashCommandArgs = null,
    [property: JsonPropertyName("slash_command_status")] string? SlashCommandStatus = null,
    [property: JsonPropertyName("attachments")] IReadOnlyList<AttachmentAnchor>? Attachments = null,
    [property: JsonPropertyName("sender_workspace_context")] SenderWorkspaceContext? SenderWorkspaceContext = null,
    [property: JsonPropertyName("audience")] IntercomMessageAudience? Audience = null,
    [property: JsonPropertyName("delivery_mode")] string? DeliveryMode = null);

/// <summary>Компенсирующее редактирование (<see cref="ChatHistoryEventKind.MessageEdited"/>).</summary>
public sealed record ChatHistoryMessageEditedPayload(
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("new_content")] string NewContent,
    [property: JsonPropertyName("reason")] string Reason);

/// <summary>Новая ветка (<see cref="ChatHistoryEventKind.ThreadForked"/>).</summary>
public sealed record ChatHistoryThreadForkedPayload(
    [property: JsonPropertyName("new_thread_id")] string NewThreadId,
    [property: JsonPropertyName("previous_thread_id")] string PreviousThreadId,
    [property: JsonPropertyName("parent_message_id")] string? ParentMessageId = null);

/// <summary>Ответ на пакет уточнений (<see cref="ChatHistoryEventKind.ClarificationAnswerSubmitted"/>).</summary>
public sealed record ChatHistoryClarificationAnswerSubmittedPayload(
    [property: JsonPropertyName("batch_id")] string BatchId,
    [property: JsonPropertyName("answers")] IReadOnlyDictionary<string, string> Answers);

/// <summary>Явная связь gutter ordinals с кодом (<see cref="ChatHistoryEventKind.MessageRangeRelated"/>, ADR 0137/0138).</summary>
public sealed record ChatHistoryMessageRangeRelatedPayload(
    [property: JsonPropertyName("thread_id")] string ThreadId,
    [property: JsonPropertyName("start_ordinal")] int StartOrdinal,
    [property: JsonPropertyName("end_ordinal")] int EndOrdinal,
    [property: JsonPropertyName("code_ref")] AttachmentAnchor CodeRef,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("ordinal_segments")] IReadOnlyList<ChatHistoryMessageOrdinalSegment>? OrdinalSegments = null);

/// <summary>Якорь T2 context card (ADR 0174 §3.1).</summary>
public sealed record SedmContextCardAnchorPayload(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("symbol")] string? Symbol = null);

/// <summary>Workline ref в SEDM payload (MLP: thread_id как workline).</summary>
public sealed record SedmWorklineRefPayload(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("label")] string? Label = null,
    [property: JsonPropertyName("intent_tag")] string? IntentTag = null);

/// <summary>Applies one-liner (0061 / ADR map).</summary>
public sealed record SedmAppliesEntryPayload(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("one_liner")] string OneLiner,
    [property: JsonPropertyName("provenance")] string? Provenance = null);

/// <summary><see cref="ChatHistoryEventKind.ContextCardMaterialized"/> payload v1.</summary>
public sealed record SedmContextCardMaterializedPayload(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("workline_id")] string WorklineId,
    [property: JsonPropertyName("anchor")] SedmContextCardAnchorPayload Anchor,
    [property: JsonPropertyName("workline")] SedmWorklineRefPayload? Workline = null,
    [property: JsonPropertyName("applies")] IReadOnlyList<SedmAppliesEntryPayload>? Applies = null,
    [property: JsonPropertyName("path_hint")] string? PathHint = null,
    [property: JsonPropertyName("risk_advisory")] string? RiskAdvisory = null,
    [property: JsonPropertyName("drill_down")] IReadOnlyList<string>? DrillDown = null,
    [property: JsonPropertyName("trigger_reason")] string? TriggerReason = null);

/// <summary>Тело intent / decision card (ADR 0173 tier A).</summary>
public sealed record SedmIntentCardBodyPayload(
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("trigger")] string? Trigger = null,
    [property: JsonPropertyName("chosen_approach")] string? ChosenApproach = null,
    [property: JsonPropertyName("selection_rationale")] string? SelectionRationale = null,
    [property: JsonPropertyName("constraints")] string? Constraints = null,
    [property: JsonPropertyName("validation_plan")] string? ValidationPlan = null);

public sealed record SedmIntentConsideredOptionPayload(
    [property: JsonPropertyName("approach")] string Approach,
    [property: JsonPropertyName("rejected_because")] string? RejectedBecause = null);

/// <summary><see cref="ChatHistoryEventKind.IntentCardRecorded"/> payload v1.</summary>
public sealed record SedmIntentCardRecordedPayload(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("author")] string Author,
    [property: JsonPropertyName("workline_id")] string WorklineId,
    [property: JsonPropertyName("card")] SedmIntentCardBodyPayload Card,
    [property: JsonPropertyName("message_id")] string? MessageId = null,
    [property: JsonPropertyName("considered")] IReadOnlyList<SedmIntentConsideredOptionPayload>? Considered = null);

public sealed record SedmDecisionFindingPayload(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("ref")] string Ref,
    [property: JsonPropertyName("summary")] string Summary);

public sealed record SedmDecisionBasisPayload(
    [property: JsonPropertyName("revision")] string? Revision = null,
    [property: JsonPropertyName("touched_paths")] IReadOnlyList<string>? TouchedPaths = null);

/// <summary><see cref="ChatHistoryEventKind.DecisionRecorded"/> payload v1.</summary>
public sealed record SedmDecisionRecordedPayload(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("author")] string Author,
    [property: JsonPropertyName("workline_id")] string WorklineId,
    [property: JsonPropertyName("card")] SedmIntentCardBodyPayload Card,
    [property: JsonPropertyName("message_id")] string? MessageId = null,
    [property: JsonPropertyName("considered")] IReadOnlyList<SedmIntentConsideredOptionPayload>? Considered = null,
    [property: JsonPropertyName("findings")] IReadOnlyList<SedmDecisionFindingPayload>? Findings = null,
    [property: JsonPropertyName("basis")] SedmDecisionBasisPayload? Basis = null,
    [property: JsonPropertyName("status")] string Status = "active");

/// <summary>Lifecycle events for decisions (stale / superseded).</summary>
public sealed record SedmDecisionLifecyclePayload(
    [property: JsonPropertyName("schema_version")] int SchemaVersion,
    [property: JsonPropertyName("workline_id")] string WorklineId,
    [property: JsonPropertyName("decision_event_id")] string DecisionEventId,
    [property: JsonPropertyName("reason")] string? Reason = null);
