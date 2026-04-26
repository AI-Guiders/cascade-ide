using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: workspace.</summary>
public partial class MainWindowViewModel
{
    string Services.IIdeMcpActions.GetSolutionInfo()
    {
        var projects = McpSolutionTree.CollectProjectPaths(Workspace.SolutionRoots).ToList();
        return IdeMcpWorkspaceOrchestrator.SerializeSolutionInfo(
            Workspace.SolutionPath,
            CurrentFilePath,
            projects,
            Workspace.SelectedSolutionItem?.FullPath);
    }

    string Services.IIdeMcpActions.GetBuildOutput()
    {
        var (bg, fg) = Services.UiThemeSnapshot.GetBuildOutputTheme();
        return IdeMcpWorkspaceOrchestrator.SerializeBuildOutput(BuildOutputPanel.BuildOutput, bg, fg);
    }

    Task<string> Services.IIdeMcpActions.GetUiModesDiagnosticsAsync() =>
        Task.FromResult(UiModeCatalog.GetDiagnosticsJson());

    async Task<string> Services.IIdeMcpActions.SearchWorkspaceTextAsync(
        string pattern,
        string? subPath,
        bool fixedString,
        string? glob,
        int maxMatches,
        string? rgPath)
    {
        var solutionPath = await UiScheduler.Default.InvokeAsync(() => Workspace.SolutionPath ?? "");
        if (string.IsNullOrWhiteSpace(solutionPath))
            return IdeMcpWorkspaceOrchestrator.SerializeWorkspaceNotLoadedError();

        var root = BreakpointsFileService.GetWorkspaceRoot(solutionPath);
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
            return IdeMcpWorkspaceOrchestrator.SerializeInvalidWorkspaceRootError();

        return await RipgrepWorkspaceSearchService.SearchAsync(
            root,
            pattern,
            subPath,
            fixedString,
            glob,
            maxMatches,
            rgPath,
            CancellationToken.None).ConfigureAwait(false);
    }

    async Task<string> Services.IIdeMcpActions.GetIdeStateAsync()
    {
        var diagnosticsJson = await ((Services.IIdeMcpActions)this).GetCurrentFileDiagnosticsAsync().ConfigureAwait(false);
        var diagnostics = IdeMcpWorkspaceOrchestrator.ParseDiagnosticsOrEmpty(diagnosticsJson);

        return await UiScheduler.Default.InvokeAsync(() =>
        {
            var buildText = IdeMcpWorkspaceOrchestrator.BuildTruncatedOutputPreview(BuildOutputPanel.BuildOutput, 2000);

            var dbg = DapDebug.GetSnapshot();
            var state = new
            {
                solution_path = Workspace.SolutionPath,
                current_file_path = CurrentFilePath,
                selected_solution_path = Workspace.SelectedSolutionItem?.FullPath,
                editor = new
                {
                    content_length = (EditorText ?? "").Length,
                    selection_start = EditorSelectionStart,
                    selection_length = EditorSelectionLength
                },
                breakpoints = new
                {
                    current_file = AllBreakpointLinesInCurrentFile,
                    total_count = dbg.Breakpoints.Count
                },
                debug = new
                {
                    position_file = dbg.StoppedFile,
                    position_line = dbg.StoppedLine,
                    has_active_session = dbg.HasActiveSession,
                    is_stopped = dbg.IsExecutionStopped,
                    stack_count = dbg.StackFrames.Count,
                    variables_count = dbg.VariableRootScopes.Sum(g => g.Roots.Count)
                },
                build = new
                {
                    is_visible = IsBuildOutputVisible,
                    output_preview = buildText,
                    binlog_path = _lastBuildBinlogPath
                },
                terminal = new { is_visible = IsTerminalVisible },
                ui_mode = UiMode,
                panels = new
                {
                    pfd_region_expanded = IsPfdRegionExpanded,
                    build_output = IsBuildOutputVisible,
                    mfd_region_expanded = IsMfdRegionExpanded,
                    git = IsGitPanelVisible,
                    instrumentation_dock = IsInstrumentationDockVisible
                },
                safety_level = SafetyLevel,
                editor_group_count = EditorGroupCount,
                agent_trace_step_count = InstrumentationPanel.AgentTraceSteps.Count,
                is_autonomous_running = Autonomous.IsAutonomousRunning,
                diagnostics,
                cockpit_surface = BuildCockpitSurfaceSnapshot()
            };
            return IdeMcpWorkspaceOrchestrator.SerializeIdeState(state);
        });
    }

    Task<string> Services.IIdeMcpActions.GetCockpitSurfaceAsync() =>
        UiScheduler.Default.InvokeAsync(() => IdeMcpWorkspaceOrchestrator.SerializeCockpitSurface(BuildCockpitSurfaceSnapshot()));

    async Task<string> Services.IIdeMcpActions.GetCodeMetricsAsync(string? scope, string? path)
    {
        var files = await UiScheduler.Default.InvokeAsync(() =>
            McpCodeMetrics.ResolveMetricFilePaths(scope, path, CurrentFilePath, Workspace.SolutionRoots)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
        return await McpCodeMetrics.ComputeMetricsJsonAsync(scope, files).ConfigureAwait(false);
    }
}
