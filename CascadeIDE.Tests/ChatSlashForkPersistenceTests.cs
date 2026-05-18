using CascadeIDE.Features.Chat;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSlashForkPersistenceTests
{
    [Fact]
    public void SlashAfterFork_PayloadThreadIdMatchesAssignedThread()
    {
        var mainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var topicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var cmdMsg = ChatMessageViewModel.CreateSlashCommand("/topic create ADR", "ADR", mainId);
        cmdMsg.AssignThread(topicId);
        cmdMsg.ApplySlashCommandResult(
            new ChatSlashCommandRunResult(true, true, "/topic create", "ADR", "Создана тема: ADR"));

        var payload = ChatHistoryPayloadMapping.ToMessagePayload(cmdMsg);
        Assert.Equal(topicId.ToString("N"), payload.ThreadId);
    }

    [Fact]
    public void TopicCreateFlow_CompositorShowsSyntheticTopic()
    {
        var mainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var topicId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var titles = new Dictionary<Guid, string> { [topicId] = "ADR" };

        var slashMsg = new ChatConversationMessage(
            Guid.NewGuid(),
            "slash_command",
            "Создана тема: ADR",
            topicId,
            null,
            0,
            "/topic create",
            "ADR",
            ChatSlashCommandStatus.Succeeded);

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [slashMsg],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: 0,
            MainThreadId: mainId,
            ActiveThreadId: topicId,
            ThreadBranchHint: "ADR",
            ThreadDisplayTitles: titles,
            ThreadForks: [new ChatThreadForkRecord(topicId, mainId, null)]));

        var topic = Assert.Single(snapshot.State.Threads, t => t.ThreadId == topicId);
        Assert.Equal("ADR", topic.Title);
        Assert.Equal(mainId, topic.ParentThreadId);
        Assert.Contains(snapshot.Layout.Overview, item => item.ThreadId == topicId);
    }
}
