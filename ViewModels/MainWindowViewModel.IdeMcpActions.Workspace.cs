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
        if (!IdeMcpWorkspaceOrchestrator.TryResolveWorkspaceRootForRipgrep(solutionPath, out var root, out var err))
            return err;

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
        var ui = await UiScheduler.Default.InvokeAsync(CaptureIdeMcpIdeStateUi);
        var state = IdeMcpWorkspaceOrchestrator.BuildIdeStatePayload(ui, diagnostics);
        return IdeMcpWorkspaceOrchestrator.SerializeIdeState(state);
    }

    /// <summary>Снимок полей UI для MCP <c>get_ide_state</c>; вызывать только с UI-потока (через <see cref="UiScheduler"/>).</summary>
    private IdeMcpIdeStateUiCapture CaptureIdeMcpIdeStateUi()
    {
        var buildText = IdeMcpWorkspaceOrchestrator.BuildTruncatedOutputPreview(BuildOutputPanel.BuildOutput, 2000);
        return new IdeMcpIdeStateUiCapture(
            Workspace.SolutionPath,
            CurrentFilePath,
            Workspace.SelectedSolutionItem?.FullPath,
            (EditorText ?? "").Length,
            EditorSelectionStart,
            EditorSelectionLength,
            AllBreakpointLinesInCurrentFile,
            DapDebug.GetSnapshot(),
            IsBuildOutputVisible,
            buildText,
            _lastBuildBinlogPath,
            IsTerminalVisible,
            UiMode,
            IsPfdRegionExpanded,
            IsMfdRegionExpanded,
            IsGitPanelVisible,
            IsInstrumentationDockVisible,
            SafetyLevel,
            EditorGroupCount,
            InstrumentationPanel.AgentTraceSteps.Count,
            Autonomous.IsAutonomousRunning,
            BuildCockpitSurfaceSnapshot());
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
