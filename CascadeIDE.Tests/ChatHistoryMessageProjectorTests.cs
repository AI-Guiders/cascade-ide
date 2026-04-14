using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatHistoryMessageProjectorTests
{
    [Fact]
    public void Project_AppliesMessageEdited_ToAssistantRow()
    {
        var sid = Guid.NewGuid();
        var userId = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        var asstId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");

        var evUser = new ChatHistoryEvent(
            Guid.NewGuid(),
            sid,
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.MessageAdded,
            $$"""{"message_id":"{{userId:N}}","role":"user","content":"hi"}""");

        var evAsst = new ChatHistoryEvent(
            Guid.NewGuid(),
            sid,
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.MessageCompleted,
            $$"""{"message_id":"{{asstId:N}}","role":"assistant","content":"old"}""");

        var evEdit = new ChatHistoryEvent(
            Guid.NewGuid(),
            sid,
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.MessageEdited,
            $$"""{"message_id":"{{asstId:N}}","new_content":"new text"}""");

        var rows = ChatHistoryMessageProjector.Project([evUser, evAsst, evEdit]);

        Assert.Equal(2, rows.Count);
        Assert.Equal("user", rows[0].Role);
        Assert.Equal("assistant", rows[1].Role);
        Assert.Equal("new text", rows[1].Content);
        Assert.Equal(asstId, rows[1].MessageId);
    }
}
