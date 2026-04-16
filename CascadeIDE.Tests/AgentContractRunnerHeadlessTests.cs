using System.Text.Json;
using Avalonia.Headless.XUnit;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services;
using CascadeIDE.Services.AgentContract;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>Паритет <c>--agent-contract</c> с MCP для CDS и полной сводки (ADR 0052).</summary>
public sealed class AgentContractRunnerHeadlessTests
{
    [AvaloniaFact]
    public void Get_cockpit_surface_matches_standalone_cds_json()
    {
        var fromRunner = AgentContractRunner.GetContractJson(IdeCommands.GetCockpitSurface);
        using var doc = JsonDocument.Parse(fromRunner);
        Assert.Equal(CockpitSurfaceSnapshotBuilder.CurrentSchemaVersion, doc.RootElement.GetProperty("schema_version").GetString());
    }

    [AvaloniaFact]
    public void Get_workspace_state_includes_cockpit_surface_same_as_get_cockpit_surface_command()
    {
        var cockpitOnly = AgentContractRunner.GetContractJson(IdeCommands.GetCockpitSurface);
        var full = AgentContractRunner.GetContractJson(IdeCommands.GetWorkspaceState);

        using var fullDoc = JsonDocument.Parse(full);
        Assert.True(fullDoc.RootElement.TryGetProperty("cockpit_surface", out var nested));
        Assert.Equal(cockpitOnly, nested.GetRawText());

        var parsed = JsonSerializer.Deserialize<CockpitSurfaceState>(cockpitOnly);
        Assert.NotNull(parsed);
        Assert.Equal(CockpitSurfaceSnapshotBuilder.CurrentSchemaVersion, parsed!.SchemaVersion);
    }
}
