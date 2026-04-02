namespace CascadeIDE.Services;

/// <summary>Коды команд IDE. Один тул ide_execute_command вызывает команду по коду; меню/хоткеи могут использовать те же коды.</summary>
public static class IdeCommands
{
    /// <summary>Список MCP-тулов, которые IDE публикует (name/description/inputSchema). returns: json.</summary>
    public const string ListTools = "list_tools";

    /// <summary>Открыть файл в редакторе IDE. args: path:string; returns: text; example: {"path":"C:\\tmp\\a.txt"}.</summary>
    public const string OpenFile = "open_file";
    /// <summary>Загрузить решение (.sln/.slnx/.slnf) и обновить дерево решения. args: path:string; returns: text; example: {"path":"D:\\Experiments\\PersonalCursorFolder\\Financial\\software\\open\\cascade-ide\\CascadeIDE.slnx"}.</summary>
    public const string LoadSolution = "load_solution";
    /// <summary>Выделить диапазон в редакторе (1-based). args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":10}.</summary>
    public const string Select = "select";
    /// <summary>Поставить брейкпоинт. args: file_path:string, line:integer, condition?:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}.</summary>
    public const string SetBreakpoint = "set_breakpoint";
    /// <summary>Снять брейкпоинт. args: file_path:string, line:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":42}.</summary>
    public const string RemoveBreakpoint = "remove_breakpoint";
    /// <summary>Показать Markdown-превью в отдельном окне. args: title:string, content:string; returns: text; example: {"title":"Plan","content":"- step 1\n- step 2"}.</summary>
    public const string ShowPreview = "show_preview";
    /// <summary>Показать превью текущего файла из редактора в отдельном окне (контент берётся из IDE). returns: text.</summary>
    public const string ShowEditorPreview = "show_editor_preview";
    /// <summary>Запросить подтверждение у пользователя. args: message:string; returns: text; example: {"message":"Продолжить?"}. Возвращает <c>ok</c>/<c>cancel</c>.</summary>
    public const string RequestConfirmation = "request_confirmation";
    /// <summary>Состояние активного редактора: файл, каретка, выделение. args: max_preview_chars?:integer; returns: json; example: {"max_preview_chars":0}.</summary>
    public const string GetEditorState = "get_editor_state";
    /// <summary>Текст активного редактора по диапазону строк (1-based). args: start_line:integer, end_line:integer; returns: json; example: {"start_line":1,"end_line":40}.</summary>
    public const string GetEditorContentRange = "get_editor_content_range";
    /// <summary>Полный текст открытого документа по пути (или текущего). Модель вкладки, не снимок темы. returns: text.</summary>
    public const string GetOpenDocumentText = "get_open_document_text";
    /// <summary>Применить текстовую правку в открытом документе. args: file_path:string, start_line:integer, start_column:integer, end_line:integer, end_column:integer, new_text:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs","start_line":1,"start_column":1,"end_line":1,"end_column":1,"new_text":"// hi\n"}.</summary>
    public const string ApplyEdit = "apply_edit";
    /// <summary>Перейти на позицию (и опционально выделить диапазон). args: file_path:string, line:integer, column:integer, end_line?:integer, end_column?:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":10,"column":1}.</summary>
    public const string GoToPosition = "go_to_position";
    /// <summary>Короткая информация о текущем решении/файле/выделении в дереве. returns: json.</summary>
    public const string GetSolutionInfo = "get_solution_info";
    /// <summary>Список файлов и дерево решения (Solution Explorer). returns: json.</summary>
    public const string GetSolutionFiles = "get_solution_files";
    /// <summary>Диагностики текущего открытого .cs (ошибки/предупреждения). returns: json.</summary>
    public const string GetCurrentFileDiagnostics = "get_current_file_diagnostics";
    /// <summary>Сборка решения (структурированный результат). returns: json.</summary>
    public const string Build = "build";
    /// <summary>Сборка решения (структурированный результат). То же, что <c>build</c>; выделено для совместимости/алиасов. returns: json.</summary>
    public const string BuildStructured = "build_structured";
    /// <summary>Запустить тесты решения. returns: json.</summary>
    public const string RunTests = "run_tests";
    /// <summary>Запустить затронутые тесты по changed_paths (или fallback на полный прогон). args: changed_paths?:string[]; returns: json; example: {"changed_paths":["a.cs","b.cs"]}.</summary>
    public const string RunAffectedTests = "run_affected_tests";
    /// <summary>Запустить code cleanup (<c>dotnet format</c>). args: include_path?:string; returns: json; example: {"include_path":"src"}.</summary>
    public const string RunCodeCleanup = "run_code_cleanup";
    /// <summary>Метрики кода (LOC/классы/методы/цикломатика). args: scope?:string, path?:string; returns: json; example: {"scope":"solution","path":"."}.</summary>
    public const string GetCodeMetrics = "get_code_metrics";
    /// <summary>Единая сводка состояния IDE (solution/editor/build/diagnostics...). returns: json.</summary>
    public const string GetWorkspaceState = "get_workspace_state";
    /// <summary>Git status в каталоге решения/workspace. returns: json.</summary>
    public const string GitStatus = "git_status";
    /// <summary>Git diff в каталоге решения/workspace. args: path?:string, staged?:boolean; returns: json; example: {"path":"README.md","staged":false}.</summary>
    public const string GitDiff = "git_diff";
    /// <summary>Git commit в каталоге решения/workspace. args: message:string, paths?:string[]; returns: text; example: {"message":"chore: update","paths":["a.txt"]}.</summary>
    public const string GitCommit = "git_commit";
    /// <summary>Git push в каталоге решения/workspace. args: remote?:string, branch?:string; returns: text; example: {"remote":"origin","branch":"main"}.</summary>
    public const string GitPush = "git_push";
    /// <summary>Текст панели «Вывод сборки» + цвета оформления. returns: json.</summary>
    public const string GetBuildOutput = "get_build_output";
    /// <summary>Передать фокус в редактор (чтобы клавиши/ввод шли в него). returns: text.</summary>
    public const string FocusEditor = "focus_editor";
    /// <summary>Как меню «Вид → Терминал» (переключатель). returns: text.</summary>
    public const string ToggleTerminal = "toggle_terminal";
    /// <summary>Как меню «Вид → Вывод сборки». returns: text.</summary>
    public const string ToggleBuildOutput = "toggle_build_output";
    /// <summary>Как меню «Вид → Обозреватель решения». returns: text.</summary>
    public const string ToggleSolutionExplorer = "toggle_solution_explorer";
    /// <summary>Явно показать/скрыть терминал (без переключения). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetTerminalVisible = "set_terminal_visible";
    /// <summary>Явно показать/скрыть журнал сборки. args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetBuildOutputVisible = "set_build_output_visible";
    /// <summary>Режим UI (как меню «Вид → Режим интерфейса»). args: mode:string; returns: text; example: {"mode":"Power"}.</summary>
    public const string SetUiMode = "set_ui_mode";

    // ——— Меню «Файл» / приложение (те же RelayCommand, что в UI)
    /// <summary>Открыть диалог выбора решения (как меню Файл → Открыть решение...). returns: text.</summary>
    public const string OpenSolutionDialog = "open_solution_dialog";
    /// <summary>Закрыть приложение (как меню Файл → Выход). returns: none.</summary>
    public const string ExitApplication = "exit_application";

    // ——— Вид: панели (явная установка + переключатели)
    /// <summary>Показать/скрыть обозреватель решения. args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetSolutionExplorerVisible = "set_solution_explorer_visible";
    /// <summary>Развернуть/свернуть чат-панель. args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetChatPanelExpanded = "set_chat_panel_expanded";
    /// <summary>Показать/скрыть панель Git (нижняя вкладка). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetGitPanelVisible = "set_git_panel_visible";
    /// <summary>Показать/скрыть док инструментирования (Events/Tests/Debug). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetInstrumentationDockVisible = "set_instrumentation_dock_visible";
    /// <summary>Переключить видимость панели Git (toggle). returns: text.</summary>
    public const string ToggleGitPanel = "toggle_git_panel";
    /// <summary>Переключить видимость дока инструментирования (toggle). returns: text.</summary>
    public const string ToggleInstrumentationDock = "toggle_instrumentation_dock";
    /// <summary>Переключить сворачивание чата (toggle). returns: text.</summary>
    public const string ToggleChatPanel = "toggle_chat_panel";

    // ——— Вид: режим (дублируют хоткеи Alt+1/2/3, Ctrl+Alt+M)
    /// <summary>Установить Focus UI mode (hotkey). returns: text.</summary>
    public const string SetFocusModeUi = "set_focus_mode";
    /// <summary>Установить Balanced UI mode (hotkey). returns: text.</summary>
    public const string SetBalancedModeUi = "set_balanced_mode";
    /// <summary>Установить Power UI mode (hotkey). returns: text.</summary>
    public const string SetPowerModeUi = "set_power_mode";
    /// <summary>Циклически переключить UI mode (hotkey). returns: text.</summary>
    public const string CycleUiMode = "cycle_ui_mode";

    // ——— Вид: тема
    /// <summary>Применить светлую тему. returns: text.</summary>
    public const string ApplyLightTheme = "apply_light_theme";
    /// <summary>Применить тёмную тему. returns: text.</summary>
    public const string ApplyDarkTheme = "apply_dark_theme";
    /// <summary>Применить тему «как Cursor». returns: text.</summary>
    public const string ApplyCursorLikeTheme = "apply_cursor_like_theme";
    /// <summary>Применить классическую Power-тему (циан). returns: text.</summary>
    public const string ApplyPowerClassicTheme = "apply_power_classic_theme";
    /// <summary>Открыть диалог выбора файла темы. returns: text.</summary>
    public const string OpenThemeFileDialog = "open_theme_file_dialog";

    // ——— Вид: язык UI
    /// <summary>Установить язык UI. args: culture:string; returns: text; example: {"culture":"ru-RU"}.</summary>
    public const string SetUiLanguage = "set_ui_language";
    /// <summary>Сбросить язык UI к системному. returns: text.</summary>
    public const string ResetUiLanguageToSystem = "reset_ui_language_to_system";

    // ——— Меню: превью, настройки, справка
    /// <summary>Открыть отдельное окно превью (Markdown). returns: text.</summary>
    public const string OpenPreviewWindow = "open_preview_window";
    /// <summary>Открыть окно настроек. returns: text.</summary>
    public const string OpenSettings = "open_settings";
    /// <summary>Показать диалог «О программе». returns: text.</summary>
    public const string About = "about";

    // ——— Тулбар: показать панели / скрыть вывод сборки
    /// <summary>Явно показать обозреватель решения (toolbar). returns: text.</summary>
    public const string ShowSolutionExplorerPanel = "show_solution_explorer_panel";
    /// <summary>Явно показать панель вывода сборки (toolbar). returns: text.</summary>
    public const string ShowBuildOutputPanel = "show_build_output_panel";
    /// <summary>Явно показать чат-панель (toolbar). returns: text.</summary>
    public const string ShowChatPanel = "show_chat_panel";
    /// <summary>Явно показать терминал (toolbar). returns: text.</summary>
    public const string ShowTerminalPanel = "show_terminal_panel";
    /// <summary>Скрыть панель вывода сборки (toolbar). returns: text.</summary>
    public const string HideBuildOutputPanel = "hide_build_output_panel";

    // ——— Тулбар: группы редакторов
    /// <summary>Одна группа редакторов (1-up). returns: text.</summary>
    public const string SetSingleEditorGroup = "set_single_editor_group";
    /// <summary>Две группы редакторов (2-up). returns: text.</summary>
    public const string SetDualEditorGroup = "set_dual_editor_group";
    /// <summary>Три группы редакторов (3-up). returns: text.</summary>
    public const string SetTripleEditorGroup = "set_triple_editor_group";

    /// <summary>Кнопка «Собрать» в тулбаре: <c>dotnet build</c> в панель вывода (не structured build). returns: text.</summary>
    public const string BuildSolutionUi = "build_solution_ui";

    // ——— Focus / Power: чат и автономный режим
    /// <summary>Создать контрольную точку (Focus). returns: text.</summary>
    public const string FocusCheckpoint = "focus_checkpoint";
    /// <summary>Откатить к последней контрольной точке (Focus). returns: text.</summary>
    public const string FocusRollback = "focus_rollback";
    /// <summary>Подтвердить текущий шаг плана (Focus). returns: text.</summary>
    public const string ConfirmFocusStep = "confirm_focus_step";
    /// <summary>Отменить текущий шаг плана (Focus). returns: text.</summary>
    public const string CancelFocusStep = "cancel_focus_step";
    /// <summary>Пояснить текущий шаг (Focus/Power). returns: text.</summary>
    public const string ExplainCurrentStep = "explain_current_step";
    /// <summary>Экстренно остановить автономные действия/выполнение (Emergency stop). returns: text.</summary>
    public const string EmergencyStop = "emergency_stop";
    /// <summary>Обновить снимок рабочего состояния (Power cockpit). returns: text.</summary>
    public const string RefreshWorkspaceSnapshot = "refresh_workspace_snapshot";
    /// <summary>Шаг трассы по индексу в <c>AgentTraceSteps</c> (0 — самый старый). args: step_index:integer; returns: text; example: {"step_index":0}.</summary>
    public const string ExplainTraceStep = "explain_trace_step";
    /// <summary>Откатить состояние по шагу трассы. returns: text.</summary>
    public const string RollbackTraceStep = "rollback_trace_step";
    /// <summary>Установить Safety L1. returns: text.</summary>
    public const string SetSafetyL1 = "set_safety_l1";
    /// <summary>Установить Safety L2. returns: text.</summary>
    public const string SetSafetyL2 = "set_safety_l2";
    /// <summary>Установить Safety L3. returns: text.</summary>
    public const string SetSafetyL3 = "set_safety_l3";
    /// <summary>Запустить автономный режим (agent run). returns: text.</summary>
    public const string StartAutonomous = "start_autonomous";
    /// <summary>Поставить автономный режим на паузу. returns: text.</summary>
    public const string PauseAutonomous = "pause_autonomous";
    /// <summary>Продолжить автономный режим после паузы. returns: text.</summary>
    public const string ResumeAutonomous = "resume_autonomous";
    /// <summary>Quick action: починить упавшие тесты. returns: text.</summary>
    public const string FixFailingTests = "fix_failing_tests";
    /// <summary>Quick action: расследовать NullReferenceException. returns: text.</summary>
    public const string InvestigateNullref = "investigate_nullref";
    /// <summary>Quick action: подготовить коммит (сводка/план/проверки). returns: text.</summary>
    public const string PrepareCommit = "prepare_commit";

    /// <summary>Кнопка отправки чата; опционально <c>message</c> — записать в поле ввода перед отправкой. returns: text.</summary>
    public const string SendChat = "send_chat";

    /// <summary>Скачать модель Ollama (как в настройках). args: model:string; returns: text; example: {"model":"qwen2.5-coder:7b"}.</summary>
    public const string InstallOllamaModel = "install_ollama_model";

    // ——— Документы (контекстное меню / док)
    /// <summary>Переоткрыть недавно закрытый документ. returns: text.</summary>
    public const string ReopenClosedDocument = "reopen_closed_document";
    /// <summary>Активировать документ (переключить вкладку). args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}.</summary>
    public const string ActivateDocument = "activate_document";
    /// <summary>Закрыть документ. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}.</summary>
    public const string CloseDocument = "close_document";
    /// <summary>Закрепить/открепить документ (pin). args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}.</summary>
    public const string TogglePinDocument = "toggle_pin_document";
    /// <summary>Переместить документ в группу 1. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}.</summary>
    public const string MoveDocumentToGroup1 = "move_document_to_group_1";
    /// <summary>Переместить документ в группу 2. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}.</summary>
    public const string MoveDocumentToGroup2 = "move_document_to_group_2";
    /// <summary>Переместить документ в группу 3. args: file_path:string; returns: text; example: {"file_path":"C:\\tmp\\a.cs"}.</summary>
    public const string MoveDocumentToGroup3 = "move_document_to_group_3";

    /// <summary>Снимок темы UI и лэйаута (включая resolved-ресурсы). returns: json.</summary>
    public const string GetUiTheme = "get_ui_theme";
    /// <summary>Применить тему UI из JSON. args: theme:string; returns: text; example: {"theme":"{\"name\":\"MyTheme\"}"}.</summary>
    public const string SetUiTheme = "set_ui_theme";
    /// <summary>Дерево UI-элементов (layout) с bounds/visibility/content. returns: json.</summary>
    public const string GetUiLayout = "get_ui_layout";
    /// <summary>Цвета под курсором (прямые и effective). returns: json.</summary>
    public const string GetColorsUnderCursor = "get_colors_under_cursor";
    /// <summary>Снимок внешнего вида контрола (под курсором или по имени). args: name?:string; returns: json; example: {"name":"BuildButton"}.</summary>
    public const string GetControlAppearance = "get_control_appearance";
    /// <summary>Изменить раскладку/позицию контрола. args: name:string, layout:string; returns: text; example: {"name":"BuildButton","layout":"{\"x\":10,\"y\":10}"}.</summary>
    public const string SetControlLayout = "set_control_layout";
    /// <summary>Установить текст в контроле ввода. args: name:string, text:string; returns: text; example: {"name":"ChatInput","text":"hi"}.</summary>
    public const string SetControlText = "set_control_text";
    /// <summary>Клик по кнопке (под курсором или по имени). args: name?:string; returns: text; example: {"name":"BuildButton"}.</summary>
    public const string ClickControl = "click_control";
    /// <summary>Отправить хоткей в контрол. args: keys:string, name?:string; returns: text; example: {"keys":"Ctrl+S"}.</summary>
    public const string SendKeys = "send_keys";
    /// <summary>Передать фокус контролу (под курсором или по имени). args: name?:string; returns: text; example: {"name":"Editor"}.</summary>
    public const string SetFocus = "set_focus";
    /// <summary>Подсветить контрол рамкой (под курсором или по имени). args: name?:string; returns: text; example: {"name":"BuildButton"}.</summary>
    public const string HighlightControl = "highlight_control";
    /// <summary>Изменить размер панели. args: panel:string, width?:integer, height?:integer; returns: text; example: {"panel":"terminal","height":300}.</summary>
    public const string SetPanelSize = "set_panel_size";
    /// <summary>Список поддерживаемых языков подсветки редактора. returns: json.</summary>
    public const string GetSupportedEditorLanguages = "get_supported_editor_languages";
    /// <summary>Показать брейкпоинты отладчика в IDE. args: breakpoints:object[]; returns: text; example: {"breakpoints":[{"file_path":"C:\\tmp\\a.cs","line":1}]}.</summary>
    public const string ShowBreakpoints = "show_breakpoints";
    /// <summary>Показать текущую позицию отладки (файл/строка). args: file_path?:string, line?:integer; returns: text; example: {"file_path":"C:\\tmp\\a.cs","line":1}.</summary>
    public const string ShowDebugPosition = "show_debug_position";
    /// <summary>Показать стек/переменные отладки в панели Debug. args: stack_frames?:object[], variables?:object[]; returns: text; example: {"stack_frames":[],"variables":[]}.</summary>
    public const string ShowDebugState = "show_debug_state";
    /// <summary>Добавить контрол в UI (Debug). args: parent_name:string, control_type:string, content?:string, name?:string; returns: text; example: {"parent_name":"Root","control_type":"TextBlock","content":"Hi"}.</summary>
    public const string AddControl = "add_control";
    /// <summary>Записать заметки агента в каталог решения. args: content:string; returns: text; example: {"content":"notes"}.</summary>
    public const string WriteAgentNotes = "write_agent_notes";
    /// <summary>Прочитать заметки агента из каталога решения. returns: text.</summary>
    public const string ReadAgentNotes = "read_agent_notes";

    /// <summary>Вставить/обновить секцию заметок агента по section_id (маркерный блок). args: section_id:string, content:string; returns: text; example: {"section_id":"active","content":"ActiveProjectId: cascade-ide"}.</summary>
    public const string UpsertAgentNotesSection = "upsert_agent_notes_section";
    /// <summary>Поиск по заметкам агента (case-insensitive) с возвратом совпадающих строк. args: query:string, head_limit?:integer; returns: json; example: {"query":"ActiveProjectId","head_limit":20}.</summary>
    public const string SearchAgentNotes = "search_agent_notes";
    /// <summary>Прочитать knowledge-файл из каталога решения. args: file_path:string; returns: text; example: {"file_path":"META/integrity-core.md"}.</summary>
    public const string ReadKnowledgeFile = "read_knowledge_file";
    /// <summary>Список knowledge-файлов в каталоге решения (опционально subdir). args: subdir?:string; returns: json; example: {"subdir":"work"}.</summary>
    public const string ListKnowledgeFiles = "list_knowledge_files";
}
