using System.Text.Json;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CockpitSurfaceSnapshotBuilderTests
{
    [Fact]
    public void Build_sets_schema_0_1_and_maps_surface_kind_docked_grid_by_default()
    {
        var vm = new MainWindowViewModel();
        var state = CockpitSurfaceSnapshotBuilder.Build(vm);

        Assert.Equal(CockpitSurfaceSnapshotBuilder.CurrentSchemaVersion, state.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(state.UiMode));
        Assert.Equal("main_window_docked_grid", state.Topology.SurfaceKind);
        Assert.False(state.Topology.MfdHostWindowOpen);
        Assert.True(state.Zones.ForwardVisible);
        Assert.Equal(vm.EffectivePresentationLine, state.PresentationEffectiveLine);
    }

    [Fact]
    public void Json_roundtrip_preserves_contract_property_names()
    {
        var state = new CockpitSurfaceState(
            SchemaVersion: "0.1",
            UiMode: "Balanced",
            PresentationEffectiveLine: "(PFD|Forward|MFD)",
            PresentationParseSuccess: true,
            Topology: new CockpitSurfaceTopology(
                "main_window_docked_grid",
                MfdHostWindowOpen: false,
                MfdColumnVisibleInMain: true),
            SecondaryShell: new CockpitSurfaceSecondaryShell("Terminal"),
            Zones: new CockpitSurfaceZones(true, true, true));

        var json = JsonSerializer.Serialize(state);
        Assert.Contains("\"schema_version\":\"0.1\"", json);
        Assert.Contains("\"ui_mode\":\"Balanced\"", json);
        Assert.Contains("\"presentation_effective_line\"", json);
        Assert.Contains("\"surface_kind\":\"main_window_docked_grid\"", json);
        Assert.Contains("\"mfd_host_window_open\":false", json);
        Assert.Contains("\"current_page\":\"Terminal\"", json);
        Assert.Contains("\"pfd_visible\":true", json);
        Assert.Contains("\"forward_visible\":true", json);
        Assert.Contains("\"mfd_visible\":true", json);

        var back = JsonSerializer.Deserialize<CockpitSurfaceState>(json);
        Assert.NotNull(back);
        Assert.Equal("0.1", back!.SchemaVersion);
        Assert.Equal("Terminal", back.SecondaryShell.CurrentPage);
    }
}
