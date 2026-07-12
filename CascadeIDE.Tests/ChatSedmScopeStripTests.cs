using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSedmScopeStripTests
{
    [Fact]
    public void BuildAgentContextPrefix_IncludesContextIntentDecision()
    {
        var strip = new ChatSedmScopeStrip(
            "Foo.cs · 0172 — habitat",
            "strip shows worklines → meta projection",
            "S1 events → append-only",
            "active",
            OpenWorklineCount: 2,
            IntentIncomplete: false);

        var prefix = strip.BuildAgentContextPrefix();
        Assert.NotNull(prefix);
        Assert.Contains("Here: Foo.cs", prefix, StringComparison.Ordinal);
        Assert.Contains("Intent:", prefix, StringComparison.Ordinal);
        Assert.Contains("Decision:", prefix, StringComparison.Ordinal);
        Assert.Contains("Other open worklines: 1", prefix, StringComparison.Ordinal);
    }

    [Fact]
    public void FromProjection_FlagsIncompleteIntent()
    {
        var workline = new SedmEventProjector.WorklineProjection(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            null,
            new SedmIntentCardRecordedPayload(
                1,
                "operator",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                new SedmIntentCardBodyPayload("outcome only", ChosenApproach: "x")),
            null,
            []);

        var strip = ChatSedmScopeStrip.FromProjection(workline, 1);
        Assert.True(strip.IntentIncomplete);
        Assert.Contains("(incomplete)", strip.FormatStripText(), StringComparison.Ordinal);
    }

    [Fact]
    public void FormatStripText_ShowsStaleDecision()
    {
        var strip = new ChatSedmScopeStrip(
            null,
            null,
            "old basis → retry",
            "stale",
            1,
            false);

        Assert.Contains("[stale]", strip.FormatStripText(), StringComparison.Ordinal);
    }
}
