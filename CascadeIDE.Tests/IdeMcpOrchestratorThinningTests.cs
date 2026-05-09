using System.Collections.ObjectModel;
using System.Text.Json;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeMcpOrchestratorThinningTests
{
    [Fact]
    public void BuildGetOpenDocumentTextResponse_no_path_returns_error()
    {
        var json = IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(null, null, [], null);
        Assert.Contains("no_path", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetOpenDocumentTextResponse_not_open_tab_returns_error()
    {
        var tabs = new[]
        {
            new IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot(@"C:\proj\a.cs", "x", false),
        };
        var json = IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(@"C:\proj\b.cs", null, tabs, null);
        Assert.Contains("not_open", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildGetOpenDocumentTextResponse_matches_tab_by_path()
    {
        var path = @"C:\proj\match.cs";
        var tabs = new[] { new IdeMcpEditorOrchestrator.OpenDocumentTabSnapshot(path, "body", true) };
        var json = IdeMcpEditorOrchestrator.BuildGetOpenDocumentTextResponse(path, null, tabs, null);
        Assert.Contains("\"body\"", json, StringComparison.Ordinal);
        Assert.Contains("is_dirty", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSolutionFilesJson_empty_collection_has_empty_arrays()
    {
        var json = IdeMcpBuildTestOrchestrator.BuildSolutionFilesJson(null, new ObservableCollection<SolutionItem>());
        Assert.Contains("\"file_entries\":[]", json, StringComparison.Ordinal);
        Assert.Contains("\"solution_tree\":[]", json, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildIdeStatePayload_maps_capture_and_nested_cockpit_surface()
    {
        var vm = new MainWindowViewModel();
        var cds = CockpitSurfaceSnapshotBuilder.Build(vm);
        var capture = new IdeMcpIdeStateUiCapture(
            @"C:\sol\a.sln",
            @"C:\sol\File.cs",
            SelectedSolutionPath: null,
            EditorTextLength: 42,
            SelectionStart: 1,
            SelectionLength: 5,
            CurrentFileBreakpoints: new[] { 10, 20 },
            DebugSnapshot: DebugSessionSnapshot.Empty,
            IsBuildOutputVisible: true,
            BuildOutputPreview: "build preview",
            BinlogPath: @"C:\logs\a.binlog",
            IsTerminalVisible: false,
            UiMode: "Power",
            IsPfdRegionExpanded: true,
            IsMfdRegionExpanded: false,
            IsGitPanelVisible: true,
            IsInstrumentationDockVisible: false,
            SafetyLevel: "A",
            EditorGroupCount: 2,
            AgentTraceStepCount: 3,
            IsAutonomousRunning: false,
            CockpitSurface: cds);
        var diag = JsonSerializer.SerializeToElement(Array.Empty<object>());
        var json = JsonSerializer.Serialize(IdeMcpWorkspaceOrchestrator.BuildIdeStatePayload(capture, diag));
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(@"C:\sol\a.sln", root.GetProperty("solution_path").GetString());
        Assert.Equal(42, root.GetProperty("editor").GetProperty("content_length").GetInt32());
        Assert.False(string.IsNullOrEmpty(root.GetProperty("cockpit_surface").GetProperty("schema_version").GetString()));
    }

    [Fact]
    public void BuildDapSnapshotUiPlan_not_stopped_resets_mfd_prim()
    {
        var s = DebugSessionSnapshot.Empty with { HasActiveSession = true, IsExecutionStopped = false };
        var p = IdeMcpDebugOrchestrator.BuildDapSnapshotUiPlan(s, mfdDebugPagePrimedForCurrentStop: true);
        Assert.False(p.MfdPrimedForCurrentStopNext);
        Assert.False(p.ActivateInstrumentationDockAndDebugStack);
        Assert.Equal(-1, p.DebugStackSelectedIndex);
    }

    [Fact]
    public void BuildDapSnapshotUiPlan_first_stop_activates_dock_and_prim()
    {
        var s = DebugSessionSnapshot.Empty with
        {
            HasActiveSession = true,
            IsExecutionStopped = true,
            StoppedFile = @"C:\proj\Stopped.cs",
            StoppedLine = 9,
            StackFrames = [("Main", @"C:\proj\Stopped.cs", 9)],
            VariablesFrameIndex = 0,
        };
        var p = IdeMcpDebugOrchestrator.BuildDapSnapshotUiPlan(s, false);
        Assert.True(p.MfdPrimedForCurrentStopNext);
        Assert.True(p.ActivateInstrumentationDockAndDebugStack);
        Assert.True(p.ShouldAttemptOpenStoppedSource);
        Assert.Equal(@"C:\proj\Stopped.cs", p.StoppedSourcePathForOpenAttempt);
        Assert.Equal(0, p.DebugStackSelectedIndex);
    }

    [Fact]
    public void BuildDapSnapshotUiPlan_subsequent_stop_does_not_activate_dock()
    {
        var s = DebugSessionSnapshot.Empty with
        {
            HasActiveSession = true,
            IsExecutionStopped = true,
            StoppedFile = @"C:\a.cs",
            StackFrames = [("F", @"C:\a.cs", 1)],
            VariablesFrameIndex = 0,
        };
        var p = IdeMcpDebugOrchestrator.BuildDapSnapshotUiPlan(s, mfdDebugPagePrimedForCurrentStop: true);
        Assert.True(p.MfdPrimedForCurrentStopNext);
        Assert.False(p.ActivateInstrumentationDockAndDebugStack);
    }

    [Fact]
    public void BuildMissingSolutionPanelSurface_aligns_reply_with_panel_wrap()
    {
        var s = IdeMcpBuildTestOrchestrator.BuildMissingSolutionPanelSurface();
        Assert.Equal(IdeMcpBuildTestOrchestrator.MissingSolutionMessage(), s.McpReplyText);
        Assert.StartsWith(s.McpReplyText + "\r\n", s.BuildOutputPanelFullText, StringComparison.Ordinal);
    }

    [Fact]
    public void FailedBuildPanelSurface_echoes_error_prefix()
    {
        var s = IdeMcpBuildTestOrchestrator.FailedBuildPanelSurface("boom");
        Assert.Equal("Error: boom", s.McpReplyText);
        Assert.Contains("boom", s.BuildOutputPanelFullText, StringComparison.Ordinal);
    }

    [Fact]
    public void PingJson_contains_expected_kind_and_ok()
    {
        using var doc = JsonDocument.Parse(IdeMcpHostOrchestrator.PingJson());
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("cascade_ide_mcp_host", doc.RootElement.GetProperty("kind").GetString());
        Assert.True(doc.RootElement.GetProperty("pid").TryGetInt32(out var pid));
        Assert.True(pid > 0);
    }

    [Fact]
    public void HybridCodebaseIndexOrchestrator_errors_are_stable_literals()
    {
        Assert.Contains("missing_query", IdeMcpHybridCodebaseIndexOrchestrator.MissingQueryJson(), StringComparison.Ordinal);
        Assert.Contains("invalid_hit_id", IdeMcpHybridCodebaseIndexOrchestrator.InvalidHitIdJson(), StringComparison.Ordinal);
        var fail = IdeMcpHybridCodebaseIndexOrchestrator.SerializeReindexFailed("x");
        Assert.Contains("reindex_failed", fail, StringComparison.Ordinal);
        Assert.Contains("\"detail\":\"x\"", fail, StringComparison.Ordinal);
    }
}
