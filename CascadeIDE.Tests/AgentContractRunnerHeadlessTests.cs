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
    public void Get_cockpit_surface_stable_slice_matches_golden_file()
    {
        var approvedPath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "AgentContract",
            "cockpit_surface_contract_slice.approved.json");
        Assert.True(File.Exists(approvedPath), $"Missing golden: {approvedPath}");

        var approved = File.ReadAllText(approvedPath);
        var expectedNorm = AgentContractCockpitContractSlice.ToStableSliceJson(approved);

        var actual = AgentContractRunner.GetContractJson(IdeCommands.GetCockpitSurface);
        var actualNorm = AgentContractCockpitContractSlice.ToStableSliceJson(actual);

        Assert.Equal(expectedNorm, actualNorm);
    }

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

    [AvaloniaFact]
    public void Get_solution_info_returns_json_with_expected_keys()
    {
        var json = AgentContractRunner.GetContractJson(IdeCommands.GetSolutionInfo);
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("solution_path", out _));
        Assert.True(doc.RootElement.TryGetProperty("current_file_path", out _));
        Assert.True(doc.RootElement.TryGetProperty("project_paths", out _));
    }

    [AvaloniaFact]
    public void Get_workspace_state_redacted_matches_on_two_calls()
    {
        var a = AgentContractWorkspaceStateRedaction.RedactForStableCompare(
            AgentContractRunner.GetContractJson(IdeCommands.GetWorkspaceState));
        var b = AgentContractWorkspaceStateRedaction.RedactForStableCompare(
            AgentContractRunner.GetContractJson(IdeCommands.GetWorkspaceState));
        Assert.Equal(a, b);
    }
}
