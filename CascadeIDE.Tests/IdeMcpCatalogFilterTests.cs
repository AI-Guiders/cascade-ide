using CascadeIDE.Services;
using Cdp.Core;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpCatalogFilterTests
{
    [Fact]
    public void ExploreKb_Shortlist_ExcludesApplyEdit()
    {
        var seed = IdeMcpAffordanceSeed.Build();
        var cold = PhaseObjectCatalog.Query(seed, CdpPhase.Explore, CdpObjectKind.Kb, CdpIntent.Cite, limit: 40);
        Assert.InRange(cold.Count, 1, 20);
        Assert.Contains(cold, h => h.Affordance.PrefixedName is "ide_route_context" or "ide_read_hot_context" or "ide_read_agent_notes");
        Assert.DoesNotContain(cold, h => h.Affordance.PrefixedName == "ide_apply_edit");
    }

    [Fact]
    public void ActCode_IncludesApplyEdit()
    {
        var seed = IdeMcpAffordanceSeed.Build();
        var hits = PhaseObjectCatalog.Query(seed, CdpPhase.Act, CdpObjectKind.Code, CdpIntent.Change, limit: 40);
        Assert.Contains(hits, h => h.Affordance.PrefixedName == "ide_apply_edit");
    }

    [Fact]
    public void Runtime_VisibleTools_MuchSmallerThanFull_AndHasMeta()
    {
        var runtime = IdeMcpRuntime.CreateForCatalogTests();
        var visible = runtime.BuildVisibleTools();
        Assert.Contains(visible, t => t.Name == "ide_context");
        Assert.Contains(visible, t => t.Name == "ide_tools");
        Assert.Contains(visible, t => t.Name == "ide_execute_command");
        Assert.True(visible.Count < 50, $"visible={visible.Count}");
    }
}
