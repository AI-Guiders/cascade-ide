using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSurfaceCompositorTests
{
    [Fact]
    public void Compose_ForkedConversation_CreatesBranchLaneAndForkEdge()
    {
        var mainThread = Guid.NewGuid();
        var branchThread = Guid.NewGuid();
        var rootMessage = new ChatConversationMessage(Guid.NewGuid(), "user", "Надо продумать ADR", mainThread, null, 0);
        var assistantMessage = new ChatConversationMessage(Guid.NewGuid(), "assistant", "Давай разложим варианты.", mainThread, null, 1);
        var branchMessage = new ChatConversationMessage(Guid.NewGuid(), "user", "Отдельно исследую Skia path", branchThread, assistantMessage.MessageId, 2);

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [rootMessage, assistantMessage, branchMessage],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: 2,
            MainThreadId: mainThread,
            ActiveThreadId: branchThread,
            ThreadBranchHint: "Ветка Skia"));

        Assert.Equal(2, snapshot.State.Threads.Count);
        Assert.Equal(2, snapshot.Layout.Lanes.Count);

        var branchLane = Assert.Single(snapshot.Layout.Lanes, lane => lane.Thread.ThreadId == branchThread);
        Assert.Equal(1, branchLane.Thread.Depth);
        Assert.Contains(branchLane.Entries, entry => entry.StartsBranch);
        Assert.Contains(snapshot.State.Edges, edge => edge.Kind == "fork");
        Assert.Contains(snapshot.Layout.Overview, item => item.ThreadId == branchThread && item.IsActive);

        var branchOverview = Assert.Single(snapshot.Layout.Overview, item => item.ThreadId == branchThread);
        Assert.Contains("Skia", branchOverview.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_SingleThreadOverview_UsesLastAssistantMessageAsSummary()
    {
        var threadId = Guid.NewGuid();
        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [
                new ChatConversationMessage(Guid.NewGuid(), "user", "Как сделать topic cards?", threadId, null, 0),
                new ChatConversationMessage(Guid.NewGuid(), "assistant", "Картотека: заголовок и короткая сводка.", threadId, null, 1)
            ],
            ActiveClarificationBatch: null,
            SelectedMessageIndex: -1,
            MainThreadId: threadId,
            ActiveThreadId: threadId,
            ThreadBranchHint: null));

        var overview = Assert.Single(snapshot.Layout.Overview);
        Assert.Contains("картотека", overview.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Compose_ActiveClarification_AddsConfirmationNodeToActiveThread()
    {
        var threadId = Guid.NewGuid();
        var batch = new ClarificationBatch(
            Guid.NewGuid(),
            [new ClarificationItem("scope", "Где показать preview?")],
            "Нужно уточнить решение");

        var snapshot = new ChatSurfaceCompositor().Compose(new ChatSurfaceIntent(
            [new ChatConversationMessage(Guid.NewGuid(), "assistant", "Есть два пути.", threadId, null, 0)],
            batch,
            SelectedMessageIndex: -1,
            MainThreadId: threadId,
            ActiveThreadId: threadId,
            ThreadBranchHint: null));

        var confirmation = Assert.Single(snapshot.State.Confirmations);
        Assert.Equal(threadId, confirmation.ThreadId);
        Assert.True(confirmation.IsActive);
        Assert.Contains("Где показать preview?", confirmation.Body);

        var lane = Assert.Single(snapshot.Layout.Lanes);
        Assert.Contains(lane.Entries, entry => entry.Kind == ChatSurfaceEntryKind.Confirmation && entry.IsPending);
        Assert.Contains(snapshot.State.Edges, edge => edge.Kind == "ask");
    }
}
