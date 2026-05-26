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
    [property: JsonPropertyName("audience")] IntercomMessageAudience? Audience = null);

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
