namespace CascadeIDE.Models.AgentChat;

/// <summary>Указатель на активную сессию Intercom в каталоге <c>chat-sessions</c>.</summary>
public sealed record ChatSessionCurrentPointer(Guid SessionId, int SchemaVersion = 1);
