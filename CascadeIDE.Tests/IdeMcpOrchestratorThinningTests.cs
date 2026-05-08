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
}
