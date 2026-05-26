using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentEnvironmentChatTraceThreadTests
{
    [Fact]
    public void Compose_AeeTraceOnMainThread_DoesNotCreateEmptyGuidTopic()
    {
        var mainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var msg = new ChatConversationMessage(
            Guid.NewGuid(),
            "assistant",
            """
            [AEE] verify deadbeef…
              Environment: 1.0s (environment)
              Status: green (L2)
            """,
            mainId,
            null,
            0,
            SlashCommandPath: "/agent verify",
            SlashCommandStatus: ChatSlashCommandStatus.Succeeded);

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [msg],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: 0,
            MainThreadId: mainId,
            ActiveThreadId: mainId,
            ThreadBranchHint: null));

        Assert.DoesNotContain(snapshot.State.Threads, t => t.ThreadId == Guid.Empty);
        Assert.Contains(snapshot.State.Threads, t => t.ThreadId == mainId);
    }

    [Fact]
    public void Compose_MessageWithEmptyThreadId_StillCreatesOrphanTopicForLegacyRows()
    {
        var mainId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var msg = new ChatConversationMessage(
            Guid.NewGuid(),
            "assistant",
            "[AEE] verify deadbeef…",
            Guid.Empty,
            null,
            0,
            SlashCommandPath: "/agent verify",
            SlashCommandStatus: ChatSlashCommandStatus.Succeeded);

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [msg],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: 0,
            MainThreadId: mainId,
            ActiveThreadId: mainId,
            ThreadBranchHint: null));

        Assert.Contains(snapshot.State.Threads, t => t.ThreadId == Guid.Empty);
    }
}
