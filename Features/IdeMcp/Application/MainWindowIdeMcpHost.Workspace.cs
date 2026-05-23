using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public string GetSolutionInfo()
    {
        var projects = McpSolutionTree.CollectProjectPaths(_host.Workspace.SolutionRoots).ToList();
        var solutionPath = _host.Workspace.SolutionPath;
        if (string.IsNullOrWhiteSpace(solutionPath))
            solutionPath = _host.ChatPanel.ResolveAttachSolutionPath() ?? "";
        return IdeMcpWorkspaceOrchestrator.SerializeSolutionInfo(
            solutionPath,
            _host.CurrentFilePath,
            projects,
            _host.Workspace.SelectedSolutionItem?.FullPath);
    }

    public string GetBuildOutput()
    {
        var (bg, fg) = Services.UiThemeSnapshot.GetBuildOutputTheme();
        return IdeMcpWorkspaceOrchestrator.SerializeBuildOutput(_host.BuildOutputPanel.BuildOutput, bg, fg);
    }

    public Task<string> GetUiModesDiagnosticsAsync() =>
        Task.FromResult(UiModeCatalog.GetDiagnosticsJson());
    public async Task<string> SearchWorkspaceTextAsync(
        string pattern,
        string? subPath,
        bool fixedString,
        string? glob,
        int maxMatches,
        string? rgPath)
    {
        var solutionPath = await UiScheduler.Default.InvokeAsync(() => _host.Workspace.SolutionPath ?? "");
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
    public async Task<string> GetIdeStateAsync()
    {
        var diagnosticsJson = await this.GetCurrentFileDiagnosticsAsync().ConfigureAwait(false);
        var diagnostics = IdeMcpWorkspaceOrchestrator.ParseDiagnosticsOrEmpty(diagnosticsJson);
        var ui = await UiScheduler.Default.InvokeAsync(CaptureIdeMcpIdeStateUi);
        var state = IdeMcpWorkspaceOrchestrator.BuildIdeStatePayload(ui, diagnostics);
        return IdeMcpWorkspaceOrchestrator.SerializeIdeState(state);
    }

    /// <summary>РЎРЅРёРјРѕРє РїРѕР»РµР№ UI РґР»СЏ MCP <c>get_ide_state</c>; РІС‹Р·С‹РІР°С‚СЊ С‚РѕР»СЊРєРѕ СЃ UI-РїРѕС‚РѕРєР° (С‡РµСЂРµР· <see cref="UiScheduler"/>).</summary>
    private IdeMcpIdeStateUiCapture CaptureIdeMcpIdeStateUi()
    {
        var buildText = IdeMcpWorkspaceOrchestrator.BuildTruncatedOutputPreview(_host.BuildOutputPanel.BuildOutput, 2000);
        return new IdeMcpIdeStateUiCapture(
            _host.Workspace.SolutionPath,
            _host.CurrentFilePath,
            _host.Workspace.SelectedSolutionItem?.FullPath,
            (_host.EditorText ?? "").Length,
            _host.EditorSelectionStart,
            _host.EditorSelectionLength,
            _host.AllBreakpointLinesInCurrentFile,
            _host.DapDebug.GetSnapshot(),
            _host.IsBuildOutputVisible,
            buildText,
            _host.McpLastBuildBinlogPath,
            _host.IsTerminalVisible,
            _host.UiMode,
            _host.IsPfdRegionExpanded,
            _host.IsMfdRegionExpanded,
            _host.IsGitPanelVisible,
            _host.IsInstrumentationDockVisible,
            _host.SafetyLevel,
            _host.EditorGroupCount,
            _host.InstrumentationPanel.AgentTraceSteps.Count,
            _host.Autonomous.IsAutonomousRunning,
            _host.BuildCockpitSurfaceSnapshot());
    }

    public Task<string> GetCockpitSurfaceAsync() =>
        UiScheduler.Default.InvokeAsync(() => IdeMcpWorkspaceOrchestrator.SerializeCockpitSurface(_host.BuildCockpitSurfaceSnapshot()));
    public async Task<string> GetCodeMetricsAsync(string? scope, string? path)
    {
        var files = await UiScheduler.Default.InvokeAsync(() =>
            McpCodeMetrics.ResolveMetricFilePaths(scope, path, _host.CurrentFilePath, _host.Workspace.SolutionRoots)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
        return await McpCodeMetrics.ComputeMetricsJsonAsync(scope, files).ConfigureAwait(false);
    }

}
