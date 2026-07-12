using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SedmTimelineBuilderTests
{
    private static readonly Guid Workline = Guid.Parse("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");

    [Fact]
    public void Build_MapsIntentAndDecisionForWorkline()
    {
        var sid = Guid.NewGuid();
        var events = new List<ChatHistoryEvent>
        {
            NewEvent(sid, ChatHistoryEventKind.IntentCardRecorded, new SedmIntentCardRecordedPayload(
                1, "operator", Workline.ToString("N"),
                new SedmIntentCardBodyPayload("outcome", Trigger: "pain", ChosenApproach: "path A"),
                Considered: [new SedmIntentConsideredOptionPayload("B", "worse")])),
            NewEvent(sid, ChatHistoryEventKind.DecisionRecorded, new SedmDecisionRecordedPayload(
                1, "agent", Workline.ToString("N"),
                new SedmIntentCardBodyPayload("decided", ChosenApproach: "path A"))),
        };

        var timeline = SedmTimelineBuilder.Build(events, Workline);
        Assert.Equal(2, timeline.Count);
        Assert.Equal("Intent", timeline[0].Title);
        Assert.Equal(ChatMessageVisualRole.SedmIntent, timeline[0].VisualRole);
        Assert.Equal("Decision", timeline[1].Title);
    }

    private static ChatHistoryEvent NewEvent(Guid sessionId, string kind, object payload) =>
        new(
            Guid.NewGuid(),
            sessionId,
            DateTimeOffset.UtcNow,
            kind,
            ChatHistoryJson.Serialize(payload),
            ThreadId: Workline.ToString("N"));
}
