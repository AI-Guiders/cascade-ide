using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatHistoryMessageProjectorTests
{
    [Fact]
    public void InferMainThreadId_uses_dominant_thread_from_events()
    {
        var sessionId = Guid.NewGuid();
        var main = Guid.Parse("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
        var side = Guid.Parse("bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb");
        var events = new List<ChatHistoryEvent>
        {
            messageEvent(sessionId, main, "one"),
            messageEvent(sessionId, main, "two"),
            messageEvent(sessionId, side, "side"),
        };

        var inferred = ChatHistoryMessageProjector.InferMainThreadId(events);
        Assert.Equal(main, inferred);
    }

    [Fact]
    public void Project_after_infer_places_new_messages_on_same_thread_as_history()
    {
        var sessionId = Guid.NewGuid();
        var thread = Guid.Parse("cccccccccccccccccccccccccccccccc");
        var events = new[] { messageEvent(sessionId, thread, "hello") };
        var inferred = ChatHistoryMessageProjector.InferMainThreadId(events);
        var rows = ChatHistoryMessageProjector.Project(events, inferred);
        Assert.Single(rows);
        Assert.Equal(thread, rows[0].ThreadId);
        Assert.Equal(inferred, thread);
    }

    private static ChatHistoryEvent messageEvent(Guid sessionId, Guid threadId, string content)
    {
        var payload = ChatHistoryPayloadMapping.ToMessagePayload(
            new ViewModels.ChatMessageViewModel("user", content, threadId: threadId));
        return new ChatHistoryEvent(
            Guid.NewGuid(),
            sessionId,
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.MessageAdded,
            ChatHistoryJson.Serialize(payload),
            ThreadId: threadId.ToString("N"));
    }
}
