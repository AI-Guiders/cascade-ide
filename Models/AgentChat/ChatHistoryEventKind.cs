namespace CascadeIDE.Models.AgentChat;

/// <summary>Типы событий append-only истории чата (ADR 0045).</summary>
public static class ChatHistoryEventKind
{
    public const string MessageAdded = "message_added";
    public const string MessageStreamDelta = "message_stream_delta";
    public const string MessageCompleted = "message_completed";
    public const string ClarificationBatchOpened = "clarification_batch_opened";
    public const string ClarificationAnswerSubmitted = "clarification_answer_submitted";
}
