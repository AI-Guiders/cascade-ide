using System.Text.Json;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// Application-level orchestrator helpers for IDE MCP workspace actions.
/// Keeps payload shaping and JSON guards out of MainWindowViewModel.
/// </summary>
public static class IdeMcpWorkspaceOrchestrator
{
    public static object BuildIdeStatePayload(
        string? solutionPath,
        string? currentFilePath,
        string? selectedSolutionPath,
        int editorTextLength,
        int? selectionStart,
        int? selectionLength,
        IReadOnlyCollection<int> currentFileBreakpoints,
        Services.DebugSessionSnapshot debugSnapshot,
        bool isBuildOutputVisible,
        string buildOutputPreview,
        string? binlogPath,
        bool isTerminalVisible,
        string? uiMode,
        bool isPfdRegionExpanded,
        bool isMfdRegionExpanded,
        bool isGitPanelVisible,
        bool isInstrumentationDockVisible,
        string? safetyLevel,
        int editorGroupCount,
        int agentTraceStepCount,
        bool isAutonomousRunning,
        JsonElement diagnostics,
        object cockpitSurface) =>
        new
        {
            solution_path = solutionPath,
            current_file_path = currentFilePath,
            selected_solution_path = selectedSolutionPath,
            editor = new
            {
                content_length = editorTextLength,
                selection_start = selectionStart ?? 0,
                selection_length = selectionLength ?? 0
            },
            breakpoints = new
            {
                current_file = currentFileBreakpoints,
                total_count = debugSnapshot.Breakpoints.Count
            },
            debug = new
            {
                position_file = debugSnapshot.StoppedFile,
                position_line = debugSnapshot.StoppedLine,
                has_active_session = debugSnapshot.HasActiveSession,
                is_stopped = debugSnapshot.IsExecutionStopped,
                stack_count = debugSnapshot.StackFrames.Count,
                variables_count = debugSnapshot.VariableRootScopes.Sum(g => g.Roots.Count)
            },
            build = new
            {
                is_visible = isBuildOutputVisible,
                output_preview = buildOutputPreview,
                binlog_path = binlogPath
            },
            terminal = new { is_visible = isTerminalVisible },
            ui_mode = uiMode,
            panels = new
            {
                pfd_region_expanded = isPfdRegionExpanded,
                build_output = isBuildOutputVisible,
                mfd_region_expanded = isMfdRegionExpanded,
                git = isGitPanelVisible,
                instrumentation_dock = isInstrumentationDockVisible
            },
            safety_level = safetyLevel,
            editor_group_count = editorGroupCount,
            agent_trace_step_count = agentTraceStepCount,
            is_autonomous_running = isAutonomousRunning,
            diagnostics,
            cockpit_surface = cockpitSurface
        };

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
