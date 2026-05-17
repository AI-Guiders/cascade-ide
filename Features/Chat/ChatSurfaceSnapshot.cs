#nullable enable
using CascadeIDE.Models.AgentChat;

namespace CascadeIDE.Features.Chat;

/// <summary>Канонический input для chat surface pipeline: сообщения, active thread и текущее состояние уточнений.</summary>
public sealed record ChatSurfaceIntent(
    IReadOnlyList<ChatConversationMessage> Messages,
    ClarificationBatch? ActiveClarificationBatch,
    int SelectedMessageIndex,
    Guid MainThreadId,
    Guid ActiveThreadId,
    string? ThreadBranchHint,
    ChatProductSpine? ProductSpine = null);

/// <summary>Плоское представление сообщения, отвязанное от UI-observable состояния.</summary>
public sealed record ChatConversationMessage(
    Guid MessageId,
    string Role,
    string Content,
    Guid ThreadId,
    Guid? ParentMessageId,
    int MessageIndex);

public sealed record ChatThreadNode(
    Guid ThreadId,
    string NodeId,
    string Title,
    bool IsMainThread,
    bool IsActive,
    Guid? ParentThreadId,
    Guid? ForkedFromMessageId,
    int Depth,
    int Order);

public sealed record ChatMessageNode(
    Guid MessageId,
    string NodeId,
    Guid ThreadId,
    Guid? ParentMessageId,
    int MessageIndex,
    string Role,
    string Content,
    bool IsSelected,
    bool StartsBranch);

public sealed record ChatConfirmationNode(
    string NodeId,
    Guid ThreadId,
    Guid BatchId,
    string Title,
    string Body,
    int ItemCount,
    bool IsActive,
    bool IsResolved);

public sealed record ChatDecisionEdge(
    string FromNodeId,
    string ToNodeId,
    string Kind);

public enum ChatSurfaceEntryKind
{
    Message = 0,
    Confirmation = 1
}

public sealed record ChatSurfaceEntry(
    ChatSurfaceEntryKind Kind,
    string NodeId,
    string Title,
    string Body,
    ChatMessageVisualRole VisualRole,
    int Order,
    int? MessageIndex = null,
    bool IsSelected = false,
    bool IsPending = false,
    bool StartsBranch = false);

public sealed record ChatThreadOverviewItem(
    Guid ThreadId,
    string Title,
    string Summary,
    bool IsActive,
    bool IsMainThread,
    int Depth,
    int ItemCount);

public sealed record ChatSurfaceLane(
    ChatThreadNode Thread,
    IReadOnlyList<ChatSurfaceEntry> Entries);

public sealed record ChatSurfaceLayout(
    IReadOnlyList<ChatThreadOverviewItem> Overview,
    IReadOnlyList<ChatSurfaceLane> Lanes);

public sealed record ChatSurfaceState(
    IReadOnlyList<ChatThreadNode> Threads,
    IReadOnlyList<ChatMessageNode> Messages,
    IReadOnlyList<ChatConfirmationNode> Confirmations,
    IReadOnlyList<ChatDecisionEdge> Edges,
    Guid ActiveThreadId,
    string ActiveThreadLabel);

public sealed record ChatSurfaceSnapshot(
    ChatSurfaceState State,
    ChatSurfaceLayout Layout,
    ChatProductSpine ProductSpine)
{
    public static ChatSurfaceSnapshot Empty { get; } = new(
        new ChatSurfaceState([], [], [], [], Guid.Empty, "Chat"),
        new ChatSurfaceLayout([], []),
        ChatProductSpine.Empty);
}
