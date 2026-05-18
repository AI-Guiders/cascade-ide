using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatThreadForkProjectorTests
{
    [Fact]
    public void Project_ReadsTypedThreadForkedPayload()
    {
        var newId = Guid.NewGuid();
        var prevId = Guid.NewGuid();
        var payload = ChatHistoryPayloadMapping.ToThreadForkedPayload(newId, prevId, null);
        var events = new[]
        {
            new ChatHistoryEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                ChatHistoryEventKind.ThreadForked,
                ChatHistoryJson.Serialize(payload)),
        };

        var forks = ChatThreadForkProjector.Project(events);
        var fork = Assert.Single(forks);
        Assert.Equal(newId, fork.NewThreadId);
        Assert.Equal(prevId, fork.PreviousThreadId);
    }
}
