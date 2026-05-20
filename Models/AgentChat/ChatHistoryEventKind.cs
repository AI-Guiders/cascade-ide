namespace CascadeIDE.Models.AgentChat;

/// <summary>Типы событий append-only истории чата (ADR 0045).</summary>
public static class ChatHistoryEventKind
{
    public const string MessageAdded = "message_added";
    public const string MessageStreamDelta = "message_stream_delta";
    public const string MessageCompleted = "message_completed";
    /// <summary>Компенсирующее событие: новый текст для существующего message_id (append-only, без перезаписи строк NDJSON).</summary>
    public const string MessageEdited = "message_edited";
    public const string ClarificationBatchOpened = "clarification_batch_opened";
    public const string ClarificationAnswerSubmitted = "clarification_answer_submitted";

    /// <summary>Новая ветка: payload — new_thread_id, previous_thread_id, optional parent_message_id.</summary>
    public const string ThreadForked = "thread_forked";

    /// <summary>Явная связь диапазона gutter-сообщений с кодом (ADR 0137): payload — <see cref="ChatHistoryMessageRangeRelatedPayload"/>.</summary>
    public const string MessageRangeRelated = "message_range_related";
}
