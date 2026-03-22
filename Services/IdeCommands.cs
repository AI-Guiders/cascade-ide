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
    /// <summary>Полный текст открытого документа по пути (или текущего). Модель вкладки, не снимок темы.</summary>
    public const string GetOpenDocumentText = "get_open_document_text";
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
    /// <summary>Как меню «Вид → Терминал» (переключатель).</summary>
    public const string ToggleTerminal = "toggle_terminal";
    /// <summary>Как меню «Вид → Вывод сборки».</summary>
    public const string ToggleBuildOutput = "toggle_build_output";
    /// <summary>Как меню «Вид → Обозреватель решения».</summary>
    public const string ToggleSolutionExplorer = "toggle_solution_explorer";
    /// <summary>Явно показать/скрыть терминал (без переключения). args: <c>visible</c> — bool.</summary>
    public const string SetTerminalVisible = "set_terminal_visible";
    /// <summary>Явно показать/скрыть журнал сборки. args: <c>visible</c> — bool.</summary>
    public const string SetBuildOutputVisible = "set_build_output_visible";
    /// <summary>Режим UI: args <c>mode</c> — Focus | Balanced | Power (как меню «Вид → Режим интерфейса»).</summary>
    public const string SetUiMode = "set_ui_mode";

    // ——— Меню «Файл» / приложение (те же RelayCommand, что в UI)
    public const string OpenSolutionDialog = "open_solution_dialog";
    public const string ExitApplication = "exit_application";

    // ——— Вид: панели (явная установка + переключатели)
    public const string SetSolutionExplorerVisible = "set_solution_explorer_visible";
    public const string SetChatPanelExpanded = "set_chat_panel_expanded";
    public const string SetGitPanelVisible = "set_git_panel_visible";
    public const string SetInstrumentationDockVisible = "set_instrumentation_dock_visible";
    public const string ToggleGitPanel = "toggle_git_panel";
    public const string ToggleInstrumentationDock = "toggle_instrumentation_dock";
    public const string ToggleChatPanel = "toggle_chat_panel";

    // ——— Вид: режим (дублируют хоткеи Alt+1/2/3, Ctrl+Alt+M)
    public const string SetFocusModeUi = "set_focus_mode";
    public const string SetBalancedModeUi = "set_balanced_mode";
    public const string SetPowerModeUi = "set_power_mode";
    public const string CycleUiMode = "cycle_ui_mode";

    // ——— Вид: тема
    public const string ApplyLightTheme = "apply_light_theme";
    public const string ApplyDarkTheme = "apply_dark_theme";
    public const string ApplyCursorLikeTheme = "apply_cursor_like_theme";
    public const string ApplyPowerClassicTheme = "apply_power_classic_theme";
    public const string OpenThemeFileDialog = "open_theme_file_dialog";

    // ——— Вид: язык UI
    public const string SetUiLanguage = "set_ui_language";
    public const string ResetUiLanguageToSystem = "reset_ui_language_to_system";

    // ——— Меню: превью, настройки, справка
    public const string OpenPreviewWindow = "open_preview_window";
    public const string OpenSettings = "open_settings";
    public const string About = "about";

    // ——— Тулбар: показать панели / скрыть вывод сборки
    public const string ShowSolutionExplorerPanel = "show_solution_explorer_panel";
    public const string ShowBuildOutputPanel = "show_build_output_panel";
    public const string ShowChatPanel = "show_chat_panel";
    public const string ShowTerminalPanel = "show_terminal_panel";
    public const string HideBuildOutputPanel = "hide_build_output_panel";

    // ——— Тулбар: группы редакторов
    public const string SetSingleEditorGroup = "set_single_editor_group";
    public const string SetDualEditorGroup = "set_dual_editor_group";
    public const string SetTripleEditorGroup = "set_triple_editor_group";

    /// <summary>Кнопка «Собрать» в тулбаре: <c>dotnet build</c> в панель вывода (не structured build).</summary>
    public const string BuildSolutionUi = "build_solution_ui";

    // ——— Focus / Power: чат и автономный режим
    public const string FocusCheckpoint = "focus_checkpoint";
    public const string FocusRollback = "focus_rollback";
    public const string ConfirmFocusStep = "confirm_focus_step";
    public const string CancelFocusStep = "cancel_focus_step";
    public const string ExplainCurrentStep = "explain_current_step";
    public const string EmergencyStop = "emergency_stop";
    public const string RefreshWorkspaceSnapshot = "refresh_workspace_snapshot";
    /// <summary>Шаг трассы по индексу в <c>AgentTraceSteps</c> (0 — самый старый). args: <c>step_index</c>.</summary>
    public const string ExplainTraceStep = "explain_trace_step";
    public const string RollbackTraceStep = "rollback_trace_step";
    public const string SetSafetyL1 = "set_safety_l1";
    public const string SetSafetyL2 = "set_safety_l2";
    public const string SetSafetyL3 = "set_safety_l3";
    public const string StartAutonomous = "start_autonomous";
    public const string PauseAutonomous = "pause_autonomous";
    public const string ResumeAutonomous = "resume_autonomous";
    public const string FixFailingTests = "fix_failing_tests";
    public const string InvestigateNullref = "investigate_nullref";
    public const string PrepareCommit = "prepare_commit";

    /// <summary>Кнопка отправки чата; опционально <c>message</c> — записать в поле ввода перед отправкой.</summary>
    public const string SendChat = "send_chat";

    /// <summary>Скачать модель Ollama (как в настройках). args: <c>model</c>.</summary>
    public const string InstallOllamaModel = "install_ollama_model";

    // ——— Документы (контекстное меню / док)
    public const string ReopenClosedDocument = "reopen_closed_document";
    public const string ActivateDocument = "activate_document";
    public const string CloseDocument = "close_document";
    public const string TogglePinDocument = "toggle_pin_document";
    public const string MoveDocumentToGroup1 = "move_document_to_group_1";
    public const string MoveDocumentToGroup2 = "move_document_to_group_2";
    public const string MoveDocumentToGroup3 = "move_document_to_group_3";

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
