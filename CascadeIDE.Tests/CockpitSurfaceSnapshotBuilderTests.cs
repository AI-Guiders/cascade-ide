using System.Text.Json;
using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CockpitSurfaceSnapshotBuilderTests
{
    [Fact]
    public void Build_sets_schema_0_3_and_maps_surface_kind_docked_grid_by_default()
    {
        var vm = new MainWindowViewModel();
        var state = CockpitSurfaceSnapshotBuilder.Build(vm);

        Assert.Equal(CockpitSurfaceSnapshotBuilder.CurrentSchemaVersion, state.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(state.UiMode));
        Assert.Equal("main_window_docked_grid", state.Topology.SurfaceKind);
        Assert.False(state.Topology.MfdHostWindowOpen);
        Assert.False(state.Topology.PfdHostWindowOpen);
        Assert.True(state.Zones.ForwardVisible);
        var pfdCount = state.Instruments.Count(x => x.SlotId == CockpitSlotIds.Pfd);
        var mfdCount = state.Instruments.Count(x => x.SlotId == CockpitSlotIds.Mfd);
        Assert.True(pfdCount <= 1 && mfdCount <= 1);
        Assert.Equal(state.Zones.PfdVisible ? 1 : 0, pfdCount);
        Assert.Equal(state.Zones.MfdVisible ? 1 : 0, mfdCount);
        if (pfdCount == 1)
        {
            Assert.Contains(
                state.Instruments,
                x => x.InstrumentId == CockpitStandardInstrumentIds.SolutionExplorerTree && x.SlotId == CockpitSlotIds.Pfd);
        }

        Assert.Equal(vm.EffectivePresentationLine, state.PresentationEffectiveLine);
    }

    [Fact]
    public void Json_roundtrip_preserves_contract_property_names()
    {
        var state = new CockpitSurfaceState(
            SchemaVersion: "0.2",
            UiMode: "Flight",
            PresentationEffectiveLine: "(PFD|Forward|MFD)",
            PresentationParseSuccess: true,
            Topology: new CockpitSurfaceTopology(
                "main_window_docked_grid",
                MfdHostWindowOpen: false,
                PfdHostWindowOpen: false,
                MfdColumnVisibleInMain: true),
            MfdShell: new CockpitSurfaceMfdShell("Terminal"),
            Zones: new CockpitSurfaceZones(
                PfdVisible: true,
                ForwardVisible: true,
                MfdVisible: true,
                PfdRequiredByPresentation: true,
                ForwardRequiredByPresentation: true,
                MfdRequiredByPresentation: true),
            Instruments:
            [
                new CockpitSurfaceInstrument("solution_explorer_tree", "pfd", "0.2"),
            ]);

        var json = JsonSerializer.Serialize(state);
        Assert.Contains("\"schema_version\":\"0.2\"", json);
        Assert.Contains("\"ui_mode\":\"Flight\"", json);
        Assert.Contains("\"presentation_effective_line\"", json);
        Assert.Contains("\"surface_kind\":\"main_window_docked_grid\"", json);
        Assert.Contains("\"mfd_host_window_open\":false", json);
        Assert.Contains("\"pfd_host_window_open\":false", json);
        Assert.Contains("\"current_page\":\"Terminal\"", json);
        Assert.Contains("\"pfd_visible\":true", json);
        Assert.Contains("\"forward_visible\":true", json);
        Assert.Contains("\"mfd_visible\":true", json);
        Assert.Contains("\"pfd_required_by_presentation\":true", json);
        Assert.Contains("\"forward_required_by_presentation\":true", json);
        Assert.Contains("\"mfd_required_by_presentation\":true", json);
        Assert.Contains("\"instruments\":[", json);
        Assert.Contains("\"instrument_id\":\"solution_explorer_tree\"", json);
        Assert.Contains("\"slot_id\":\"pfd\"", json);

        var back = JsonSerializer.Deserialize<CockpitSurfaceState>(json);
        Assert.NotNull(back);
        Assert.Equal("0.2", back!.SchemaVersion);
        Assert.Equal("Terminal", back.MfdShell.CurrentPage);
        Assert.Single(back.Instruments);
    }

    /// <summary>
    /// Паритет MCP <c>ide_get_ide_state</c> с CDS: вложенный объект сериализуется так же, как отдельный снимок.
    /// </summary>
    [Fact]
    public void Workspace_state_shape_serializes_cockpit_surface_like_standalone_snapshot()
    {
        var vm = new MainWindowViewModel();
        var cockpit = vm.BuildCockpitSurfaceSnapshot();
        var wrapped = new { cockpit_surface = cockpit };
        var json = JsonSerializer.Serialize(wrapped);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("cockpit_surface", out var el));
        var roundtrip = JsonSerializer.Deserialize<CockpitSurfaceState>(el.GetRawText());
        Assert.NotNull(roundtrip);
        Assert.Equal(cockpit.SchemaVersion, roundtrip!.SchemaVersion);
        Assert.Equal(JsonSerializer.Serialize(cockpit), JsonSerializer.Serialize(roundtrip));
    }
}
