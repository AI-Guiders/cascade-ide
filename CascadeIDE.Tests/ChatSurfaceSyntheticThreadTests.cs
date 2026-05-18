using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSurfaceSyntheticThreadTests
{
    [Fact]
    public void Compose_DisplayTitleWithoutMessages_IncludesThread()
    {
        var mainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var newTopicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var titles = new Dictionary<Guid, string> { [newTopicId] = "Новая тема" };

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: -1,
            MainThreadId: mainId,
            ActiveThreadId: newTopicId,
            ThreadBranchHint: "Новая тема",
            ThreadDisplayTitles: titles,
            ThreadForks: [new ChatThreadForkRecord(newTopicId, mainId, null)]));

        Assert.Contains(snapshot.State.Threads, t => t.ThreadId == newTopicId);
        Assert.Contains(snapshot.State.Threads, t => t.ThreadId == mainId);
        var topic = Assert.Single(snapshot.State.Threads, t => t.ThreadId == newTopicId);
        Assert.Equal("Новая тема", topic.Title);
        Assert.Equal(mainId, topic.ParentThreadId);
    }

    [Fact]
    public void Compose_ThreadForkWithoutMessages_ParentFromPreviousThread()
    {
        var mainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var branchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var root = new ChatConversationMessage(Guid.NewGuid(), "user", "root", mainId, null, 0);

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [root],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: 0,
            MainThreadId: mainId,
            ActiveThreadId: branchId,
            ThreadBranchHint: null,
            ThreadForks: [new ChatThreadForkRecord(branchId, mainId, null)]));

        var branch = Assert.Single(snapshot.State.Threads, t => t.ThreadId == branchId);
        Assert.Equal(mainId, branch.ParentThreadId);
    }
}
