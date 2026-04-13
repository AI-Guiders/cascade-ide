using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Features.Instrumentation;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: workspace.</summary>
public partial class MainWindowViewModel
{
    string Services.IIdeMcpActions.GetSolutionInfo()
    {
        var path = Workspace.SolutionPath ?? "";
        var current = CurrentFilePath ?? "";
        var projects = McpSolutionTree.CollectProjectPaths(Workspace.SolutionRoots).ToList();
        var selected = Workspace.SelectedSolutionItem?.FullPath ?? "";
        return JsonSerializer.Serialize(new { solution_path = path, current_file_path = current, project_paths = projects, selected_solution_path = selected });
    }

    string Services.IIdeMcpActions.GetBuildOutput()
    {
        var (bg, fg) = Services.UiThemeSnapshot.GetBuildOutputTheme();
        return JsonSerializer.Serialize(new { text = BuildOutputPanel.BuildOutput ?? "", theme = new { background = bg, foreground = fg } });
    }

    Task<string> Services.IIdeMcpActions.GetUiModesDiagnosticsAsync() =>
        Task.FromResult(UiModeCatalog.GetDiagnosticsJson());

    async Task<string> Services.IIdeMcpActions.GetWorkspaceStateAsync()
    {
        var diagnosticsJson = await ((Services.IIdeMcpActions)this).GetCurrentFileDiagnosticsAsync().ConfigureAwait(false);
        JsonElement diagnostics;
        try { diagnostics = JsonSerializer.Deserialize<JsonElement>(diagnosticsJson); }
        catch { diagnostics = JsonSerializer.SerializeToElement(Array.Empty<object>()); }

        return await UiScheduler.Default.InvokeAsync(() =>
        {
            var buildText = BuildOutputPanel.BuildOutput ?? "";
            if (buildText.Length > 2000)
                buildText = buildText[..2000] + "\n... (output truncated)";

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
                    debugger_count = _debuggerBreakpoints.Count
                },
                debug = new
                {
                    position_file = DebugPositionFile,
                    position_line = DebugPositionLine,
                    stack_count = InstrumentationPanel.DebugStackFrames.Count,
                    variables_count = InstrumentationPanel.DebugVariables.Count
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
                    solution_explorer = IsSolutionExplorerVisible,
                    build_output = IsBuildOutputVisible,
                    chat_expanded = IsChatPanelExpanded,
                    git = IsGitPanelVisible,
                    instrumentation_dock = IsInstrumentationDockVisible
                },
                safety_level = SafetyLevel,
                editor_group_count = EditorGroupCount,
                agent_trace_step_count = InstrumentationPanel.AgentTraceSteps.Count,
                is_autonomous_running = Autonomous.IsAutonomousRunning,
                diagnostics
            };
            return JsonSerializer.Serialize(state);
        });
    }

    async Task<string> Services.IIdeMcpActions.GetCodeMetricsAsync(string? scope, string? path)
    {
        var files = await UiScheduler.Default.InvokeAsync(() =>
            McpCodeMetrics.ResolveMetricFilePaths(scope, path, CurrentFilePath, Workspace.SolutionRoots)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
        return await McpCodeMetrics.ComputeMetricsJsonAsync(scope, files).ConfigureAwait(false);
    }
}
