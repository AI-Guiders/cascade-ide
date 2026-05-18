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

    [Fact]
    public void Project_DedupesDuplicateNewThreadIds()
    {
        var id = Guid.NewGuid();
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var events = new[]
        {
            ForkEvent(id, a),
            ForkEvent(id, b),
        };

        var forks = ChatThreadForkProjector.Project(events);
        var fork = Assert.Single(forks);
        Assert.Equal(b, fork.PreviousThreadId);
    }

    [Fact]
    public void DedupeByNewThread_KeepsLastForkPerBranch()
    {
        var id = Guid.NewGuid();
        var first = new ChatThreadForkRecord(id, Guid.NewGuid(), null);
        var second = new ChatThreadForkRecord(id, Guid.NewGuid(), Guid.NewGuid());

        var deduped = ChatThreadForkProjector.DedupeByNewThread([first, second]);
        var fork = Assert.Single(deduped);
        Assert.Equal(second.PreviousThreadId, fork.PreviousThreadId);
    }

    private static ChatHistoryEvent ForkEvent(Guid newId, Guid previousId)
    {
        var payload = ChatHistoryPayloadMapping.ToThreadForkedPayload(newId, previousId, null);
        return new ChatHistoryEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            ChatHistoryEventKind.ThreadForked,
            ChatHistoryJson.Serialize(payload));
    }
}
