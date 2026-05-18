using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatHistoryPayloadTests
{
    [Fact]
    public void MessagePayload_RoundTrip_UsesSnakeCaseKeys()
    {
        var vm = new ChatMessageViewModel("user", "hello", threadId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var payload = ChatHistoryPayloadMapping.ToMessagePayload(vm);
        var json = ChatHistoryJson.Serialize(payload);

        Assert.Contains("\"message_id\"", json);
        Assert.Contains("\"thread_id\"", json);

        var back = System.Text.Json.JsonSerializer.Deserialize<ChatHistoryMessagePayload>(json, ChatHistoryJson.Options);
        Assert.NotNull(back);
        Assert.Equal(payload.MessageId, back!.MessageId);
        Assert.Equal("hello", back.Content);
    }

    [Fact]
    public void Projector_ReadsTypedMessagePayload()
    {
        var sid = Guid.NewGuid();
        var userId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        var payload = ChatHistoryPayloadMapping.ToMessagePayload(
            new ChatMessageViewModel("user", "hi", messageId: userId));

        var ev = new ChatHistoryEvent(
            Guid.NewGuid(),
            sid,
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.MessageAdded,
            ChatHistoryJson.Serialize(payload));

        var defaultThread = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        var rows = ChatHistoryMessageProjector.Project([ev], defaultThread);

        Assert.Single(rows);
        Assert.Equal(userId, rows[0].MessageId);
        Assert.Equal("hi", rows[0].Content);
    }

    [Fact]
    public void ThreadForkedPayload_SerializesSnakeCase()
    {
        var payload = ChatHistoryPayloadMapping.ToThreadForkedPayload(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"));
        var json = ChatHistoryJson.Serialize(payload);
        Assert.Contains("\"new_thread_id\"", json);
        Assert.Contains("\"parent_message_id\"", json);
    }
}
