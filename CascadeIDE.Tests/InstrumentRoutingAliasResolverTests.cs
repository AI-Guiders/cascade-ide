using CascadeIDE.Cockpit.Composition.HostSurface;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class InstrumentRoutingAliasResolverTests
{
    [Theory]
    [InlineData("solution_explorer", "solution_explorer_tree")]
    [InlineData("workspace_map", "workspace_navigation_map")]
    [InlineData("workspace_health", "workspace_health_status_v1")]
    [InlineData("SOLUTION_EXPLORER", "solution_explorer_tree")]
    public void TryResolve_accepts_public_aliases(string alias, string expectedCanonical)
    {
        Assert.True(InstrumentRoutingAliasResolver.TryResolve(alias, out var id));
        Assert.Equal(expectedCanonical, id);
    }

    [Fact]
    public void TryResolve_accepts_canonical_ids()
    {
        Assert.True(
            InstrumentRoutingAliasResolver.TryResolve(CockpitStandardInstrumentIds.WorkspaceNavigationMap, out var id));
        Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, id);
    }

    [Fact]
    public void TryResolve_rejects_unknown()
    {
        Assert.False(InstrumentRoutingAliasResolver.TryResolve("not_an_instrument", out _));
    }
}
