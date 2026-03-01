namespace CascadeIDE.Services;

/// <summary>Коды команд IDE. Один тул ide_execute_command вызывает команду по коду; меню/хоткеи могут использовать те же коды.</summary>
public static class IdeCommands
{
    public const string OpenFile = "open_file";
    public const string LoadSolution = "load_solution";
    public const string Select = "select";
    public const string SetBreakpoint = "set_breakpoint";
    public const string RemoveBreakpoint = "remove_breakpoint";
    public const string ShowPreview = "show_preview";
    public const string ShowEditorPreview = "show_editor_preview";
    public const string RequestConfirmation = "request_confirmation";
    public const string GetEditorState = "get_editor_state";
    public const string GetEditorContentRange = "get_editor_content_range";
    public const string ApplyEdit = "apply_edit";
    public const string GoToPosition = "go_to_position";
    public const string GetSolutionInfo = "get_solution_info";
    public const string GetSolutionFiles = "get_solution_files";
    public const string GetCurrentFileDiagnostics = "get_current_file_diagnostics";
    public const string Build = "build";
    public const string BuildStructured = "build_structured";
    public const string RunTests = "run_tests";
    public const string RunAffectedTests = "run_affected_tests";
    public const string RunCodeCleanup = "run_code_cleanup";
    public const string GetCodeMetrics = "get_code_metrics";
    public const string GetWorkspaceState = "get_workspace_state";
    public const string GitStatus = "git_status";
    public const string GitDiff = "git_diff";
    public const string GitCommit = "git_commit";
    public const string GitPush = "git_push";
    public const string GetBuildOutput = "get_build_output";
    public const string FocusEditor = "focus_editor";
    public const string GetUiTheme = "get_ui_theme";
    public const string SetUiTheme = "set_ui_theme";
    public const string GetUiLayout = "get_ui_layout";
    public const string GetColorsUnderCursor = "get_colors_under_cursor";
    public const string GetControlAppearance = "get_control_appearance";
    public const string SetControlLayout = "set_control_layout";
    public const string SetControlText = "set_control_text";
    public const string ClickControl = "click_control";
    public const string SendKeys = "send_keys";
    public const string SetFocus = "set_focus";
    public const string HighlightControl = "highlight_control";
    public const string SetPanelSize = "set_panel_size";
    public const string GetSupportedEditorLanguages = "get_supported_editor_languages";
    public const string ShowBreakpoints = "show_breakpoints";
    public const string ShowDebugPosition = "show_debug_position";
    public const string ShowDebugState = "show_debug_state";
    public const string AddControl = "add_control";
    public const string WriteAgentNotes = "write_agent_notes";
    public const string ReadAgentNotes = "read_agent_notes";
}
