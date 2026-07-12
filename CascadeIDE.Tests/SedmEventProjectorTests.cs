using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SedmEventProjectorTests
{
    [Fact]
    public void Project_RoundTripsContextIntentAndDecision()
    {
        var workline = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        var decisionId = Guid.Parse("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB");
        var sid = Guid.NewGuid();

        var events = new List<ChatHistoryEvent>
        {
            NewEvent(sid, ChatHistoryEventKind.ContextCardMaterialized, new SedmContextCardMaterializedPayload(
                1,
                workline.ToString("N"),
                new SedmContextCardAnchorPayload("Features/Chat/Foo.cs", "Bar"),
                TriggerReason: "attach")),
            NewEvent(sid, ChatHistoryEventKind.IntentCardRecorded, new SedmIntentCardRecordedPayload(
                1,
                "operator",
                workline.ToString("N"),
                new SedmIntentCardBodyPayload(
                    "Strip shows open worklines",
                    Trigger: "switch loses tail",
                    ChosenApproach: "meta projection",
                    SelectionRationale: "preserves head"),
                Considered: [new SedmIntentConsideredOptionPayload("New Chat", "flat")])),
            NewEvent(sid, decisionId, ChatHistoryEventKind.DecisionRecorded, new SedmDecisionRecordedPayload(
                1,
                "agent",
                workline.ToString("N"),
                new SedmIntentCardBodyPayload("S1 events in log", ChosenApproach: "append-only"),
                Basis: new SedmDecisionBasisPayload("git:abc", ["Features/Chat/Foo.cs"]),
                Findings: [new SedmDecisionFindingPayload("adr", "0172", "G2 scope strip")])),
        };

        var projection = SedmEventProjector.Project(events, workline, openWorklineCount: 2);
        var wl = SedmEventProjector.ResolveWorkline(projection, workline);

        Assert.NotNull(wl.ContextCard);
        Assert.Equal("Features/Chat/Foo.cs", wl.ContextCard!.Anchor.Path);
        Assert.NotNull(wl.IntentCard);
        Assert.NotNull(wl.ActiveDecision);
        Assert.Equal("active", wl.ActiveDecision!.Status);
        Assert.Single(wl.DecisionHistory);
    }

    [Fact]
    public void Project_MarksDecisionStale()
    {
        var workline = Guid.Parse("CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC");
        var decisionId = Guid.Parse("DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD");
        var sid = Guid.NewGuid();

        var events = new List<ChatHistoryEvent>
        {
            NewEvent(sid, decisionId, ChatHistoryEventKind.DecisionRecorded, new SedmDecisionRecordedPayload(
                1,
                "agent",
                workline.ToString("N"),
                new SedmIntentCardBodyPayload("old basis"))),
            NewEvent(sid, ChatHistoryEventKind.DecisionMarkedStale, new SedmDecisionLifecyclePayload(
                1,
                workline.ToString("N"),
                decisionId.ToString("N"),
                "path_touch")),
        };

        var wl = SedmEventProjector.ResolveWorkline(SedmEventProjector.Project(events, workline), workline);
        Assert.Null(wl.ActiveDecision);
        Assert.Equal("stale", wl.DecisionHistory[0].Status);
    }

    [Fact]
    public void IsSameContextCard_CoalescesDuplicateAnchor()
    {
        var workline = Guid.Parse("EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
        var left = new SedmContextCardMaterializedPayload(
            1,
            workline.ToString("N"),
            new SedmContextCardAnchorPayload("a.cs"),
            TriggerReason: "attach");
        var right = left with { TriggerReason = "workline_switch" };
        Assert.True(SedmEventProjector.IsSameContextCard(left, right));
    }

    private static ChatHistoryEvent NewEvent(Guid sessionId, string kind, object payload) =>
        NewEvent(sessionId, Guid.NewGuid(), kind, payload);

    private static ChatHistoryEvent NewEvent(Guid sessionId, Guid eventId, string kind, object payload) =>
        new(
            eventId,
            sessionId,
            DateTimeOffset.UtcNow,
            kind,
            ChatHistoryJson.Serialize(payload));
}
