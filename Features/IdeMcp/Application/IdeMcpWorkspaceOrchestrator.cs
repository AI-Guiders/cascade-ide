using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using CascadeIDE.Contracts;
using CascadeIDE.Features.Workspace.Application;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP workspace actions.
/// Keeps payload shaping and JSON guards out of MainWindowViewModel.
/// </summary>
[ApplicationOrchestrator]
public static class IdeMcpWorkspaceOrchestrator
{
    public static object BuildIdeStatePayload(IdeMcpIdeStateUiCapture ui, JsonElement diagnostics)
    {
        var d = ui.DebugSnapshot;
        return new
        {
            solution_path = ui.SolutionPath,
            current_file_path = ui.CurrentFilePath,
            selected_solution_path = ui.SelectedSolutionPath,
            editor = new
            {
                content_length = ui.EditorTextLength,
                selection_start = ui.SelectionStart ?? 0,
                selection_length = ui.SelectionLength ?? 0
            },
            breakpoints = new
            {
                current_file = ui.CurrentFileBreakpoints,
                total_count = d.Breakpoints.Count
            },
            debug = new
            {
                position_file = d.StoppedFile,
                position_line = d.StoppedLine,
                has_active_session = d.HasActiveSession,
                is_stopped = d.IsExecutionStopped,
                stack_count = d.StackFrames.Count,
                variables_count = d.VariableRootScopes.Sum(g => g.Roots.Count)
            },
            build = new
            {
                is_visible = ui.IsBuildOutputVisible,
                output_preview = ui.BuildOutputPreview,
                binlog_path = ui.BinlogPath
            },
            terminal = new { is_visible = ui.IsTerminalVisible },
            ui_mode = ui.UiMode,
            panels = new
            {
                pfd_region_expanded = ui.IsPfdRegionExpanded,
                build_output = ui.IsBuildOutputVisible,
                mfd_region_expanded = ui.IsMfdRegionExpanded,
                git = ui.IsGitPanelVisible,
                instrumentation_dock = ui.IsInstrumentationDockVisible
            },
            safety_level = ui.SafetyLevel,
            editor_group_count = ui.EditorGroupCount,
            agent_trace_step_count = ui.AgentTraceStepCount,
            is_autonomous_running = ui.IsAutonomousRunning,
            diagnostics,
            cockpit_surface = ui.CockpitSurface
        };
    }

    public static string SerializeIdeState(object state) =>
        JsonSerializer.Serialize(state);

    public static string SerializeCockpitSurface(object snapshot) =>
        JsonSerializer.Serialize(snapshot);

    public static string SerializeSolutionInfo(
        string? solutionPath,
        string? currentFilePath,
        IReadOnlyList<string> projectPaths,
        string? selectedSolutionPath) =>
        JsonSerializer.Serialize(new
        {
            solution_path = solutionPath ?? "",
            current_file_path = currentFilePath ?? "",
            project_paths = projectPaths,
            selected_solution_path = selectedSolutionPath ?? ""
        });

    public static string SerializeBuildOutput(string? text, string background, string foreground) =>
        JsonSerializer.Serialize(new
        {
            text = text ?? "",
            theme = new
            {
                background,
                foreground
            }
        });

    public static string SerializeWorkspaceNotLoadedError() =>
        JsonSerializer.Serialize(new { error = "No workspace loaded." });

    public static string SerializeInvalidWorkspaceRootError() =>
        JsonSerializer.Serialize(new { error = "Invalid workspace root." });

    /// <summary>
    /// Корень каталога для MCP <c>search_workspace_text</c> (как у палитры GoTo): из пути решения через <see cref="BreakpointsFileService.GetWorkspaceRoot"/>.
    /// </summary>
    public static bool TryResolveWorkspaceRootForRipgrep(
        string? solutionPathTrimmedOrEmpty,
        [NotNullWhen(true)] out string? root,
        out string errorJson)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(solutionPathTrimmedOrEmpty))
        {
            errorJson = SerializeWorkspaceNotLoadedError();
            return false;
        }

        if (!WorkspaceBreakpointsRootPresentation.TryResolveExistingDirectory(solutionPathTrimmedOrEmpty, out var candidate))
        {
            errorJson = SerializeInvalidWorkspaceRootError();
            return false;
        }

        root = candidate;
        errorJson = "";
        return true;
    }

    public static JsonElement ParseDiagnosticsOrEmpty(string diagnosticsJson)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(diagnosticsJson);
        }
        catch
        {
            return JsonSerializer.SerializeToElement(Array.Empty<object>());
        }
    }

    public static string BuildTruncatedOutputPreview(string? buildOutput, int maxChars)
    {
        var text = buildOutput ?? "";
        return text.Length > maxChars ? text[..maxChars] + "\n... (output truncated)" : text;
    }
}
