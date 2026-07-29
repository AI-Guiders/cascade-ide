#nullable enable
using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentChromeHintDensityPolicyTests
{
    [Fact]
    public void Collapse_empty_returns_empty()
    {
        var r = AgentChromeHintDensityPolicy.Collapse(Array.Empty<AgentChromeHintDensityPolicy.Hint>());
        Assert.Empty(r.VisibleLines);
        Assert.Equal(0, r.HiddenCount);
        Assert.Null(r.OverflowLine);
    }

    [Fact]
    public void Collapse_under_cap_shows_all_no_overflow()
    {
        var hints = new List<AgentChromeHintDensityPolicy.Hint>
        {
            AgentChromeHintDensityPolicy.From("plan", "plan · focus")!.Value,
            AgentChromeHintDensityPolicy.From("pressure", "pressure · ARMED")!.Value,
        };
        var r = AgentChromeHintDensityPolicy.Collapse(hints);
        Assert.Equal(2, r.VisibleLines.Count);
        Assert.Equal("pressure · ARMED", r.VisibleLines[0]);
        Assert.Equal("plan · focus", r.VisibleLines[1]);
        Assert.Null(r.OverflowLine);
    }

    [Fact]
    public void Collapse_over_cap_keeps_priority_and_overflow()
    {
        var hints = new List<AgentChromeHintDensityPolicy.Hint>
        {
            AgentChromeHintDensityPolicy.From("learn", "learn · 14")!.Value,
            AgentChromeHintDensityPolicy.From("arch", "arch · as_built")!.Value,
            AgentChromeHintDensityPolicy.From("review", "review · ×25")!.Value,
            AgentChromeHintDensityPolicy.From("plan", "plan · focus")!.Value,
            AgentChromeHintDensityPolicy.From("pressure", "pressure · ARMED")!.Value,
            AgentChromeHintDensityPolicy.From("ignite", "ignite · await")!.Value,
            AgentChromeHintDensityPolicy.From("onboard", "onboard · map")!.Value,
        };
        var r = AgentChromeHintDensityPolicy.Collapse(hints, maxVisible: 3);
        Assert.Equal(3, r.VisibleLines.Count);
        Assert.Equal("pressure · ARMED", r.VisibleLines[0]);
        Assert.Equal("ignite · await", r.VisibleLines[1]);
        Assert.Equal("plan · focus", r.VisibleLines[2]);
        Assert.Equal(4, r.HiddenCount);
        Assert.Equal("+4 more · SoftOrgan latches", r.OverflowLine);
    }

    [Fact]
    public void From_blank_is_null()
    {
        Assert.Null(AgentChromeHintDensityPolicy.From("plan", "  "));
        Assert.Null(AgentChromeHintDensityPolicy.From("plan", null));
    }
}
