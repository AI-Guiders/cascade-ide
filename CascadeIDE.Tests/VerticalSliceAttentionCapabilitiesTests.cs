using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services.Capabilities;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class VerticalSliceAttentionCapabilitiesTests : IDisposable
{
    public VerticalSliceAttentionCapabilitiesTests() =>
        AttentionZonePanelRuntime.ResetToCodeDefaults();

    public void Dispose() =>
        AttentionZonePanelRuntime.ResetToCodeDefaults();

    [Fact]
    public void UiChrome_module_registers_solution_explorer_surface_consistent_with_runtime()
    {
        var registry = new SimpleCapabilityRegistry();
        new UiChromeCapabilitiesModule().Register(registry);
        var map = registry.BuildMap();

        Assert.Single(map.UiSurfaces);
        var s = map.UiSurfaces[0];
        Assert.Equal(CapabilityIds.UiChrome.SolutionExplorerSurface, s.Id);
        Assert.Equal(AttentionZoneCanonicalIds.Pfd, s.PrimaryAttentionZoneId);
        Assert.Equal(AttentionPanelCanonicalIds.SolutionExplorer, s.HostAttentionPanelId);
        Assert.Null(CapabilityAttentionConsistency.TryGetUiSurfaceIssue(s));
    }
}
