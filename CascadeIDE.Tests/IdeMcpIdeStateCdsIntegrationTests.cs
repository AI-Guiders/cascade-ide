using System.Text.Json;
using Avalonia.Headless.XUnit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Паритет MCP <c>ide_get_ide_state</c> с CDS: поле <c>cockpit_surface</c> совпадает с <see cref="MainWindowViewModel.BuildCockpitSurfaceSnapshot"/>.
/// Требует headless Avalonia (<see cref="AvaloniaFact"/>), т.к. <see cref="IIdeMcpActions.GetIdeStateAsync"/> маршалит на UI-поток.
/// </summary>
public sealed class IdeMcpIdeStateCdsIntegrationTests
{
    [AvaloniaFact]
    public async Task Get_ide_state_cockpit_surface_matches_BuildCockpitSurfaceSnapshot()
    {
        var vm = new MainWindowViewModel();
        IIdeMcpActions mcp = vm;

        var json = await mcp.GetIdeStateAsync();

        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("cockpit_surface", out var cockpitEl));

        var expectedJson = JsonSerializer.Serialize(vm.BuildCockpitSurfaceSnapshot());
        Assert.Equal(expectedJson, cockpitEl.GetRawText());

        var fromWorkspace = JsonSerializer.Deserialize<CockpitSurfaceState>(cockpitEl.GetRawText());
        var direct = vm.BuildCockpitSurfaceSnapshot();
        Assert.NotNull(fromWorkspace);
        Assert.Equal(direct.SchemaVersion, fromWorkspace!.SchemaVersion);
        Assert.Equal(direct.Topology.SurfaceKind, fromWorkspace.Topology.SurfaceKind);
        Assert.Equal(direct.Instruments.Count, fromWorkspace.Instruments.Count);
    }
}
