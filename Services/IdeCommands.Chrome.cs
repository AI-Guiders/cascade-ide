namespace CascadeIDE.Services;

/// <summary>Команды меню «Файл», «Вид», тулбара, темы и языка UI (partial IdeCommands).</summary>
public static partial class IdeCommands
{
    // ——— Меню «Файл» / приложение (те же RelayCommand, что в UI)
    /// <summary>Открыть диалог выбора решения (как меню Файл → Открыть решение...). returns: text.</summary>
    public const string OpenSolutionDialog = "open_solution_dialog";
    /// <summary>Открыть диалог выбора папки как workspace (как меню Файл → Открыть папку...). returns: text.</summary>
    public const string OpenFolderDialog = "open_folder_dialog";
    /// <summary>Открыть диалог выбора файла и показать его в редакторе (как меню Файл → Открыть файл...). returns: text.</summary>
    public const string OpenFileDialog = "open_file_dialog";
    /// <summary>Закрыть приложение (как меню Файл → Выход). returns: none.</summary>
    public const string ExitApplication = "exit_application";

    // ——— Вид: панели (явная установка + переключатели)
    /// <summary>Развернуть/свернуть регион Pfd в main grid (дерево решения в зоне Pfd). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetPfdRegionExpanded = "set_pfd_region_expanded";
    /// <summary>Развернуть/свернуть регион Mfd в main grid. args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetMfdRegionExpanded = "set_mfd_region_expanded";
    /// <summary>Показать/скрыть панель Git (нижняя вкладка). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetGitPanelVisible = "set_git_panel_visible";
    /// <summary>Показать/скрыть док инструментирования (Events/Tests/Debug). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetInstrumentationDockVisible = "set_instrumentation_dock_visible";
    /// <summary>Переключить видимость панели Git (toggle). returns: text.</summary>
    public const string ToggleGitPanel = "toggle_git_panel";
    /// <summary>Переключить видимость дока инструментирования (toggle). returns: text.</summary>
    public const string ToggleInstrumentationDock = "toggle_instrumentation_dock";
    /// <summary>Переключить развёрнут/свёрнут регион Mfd (toggle). returns: text.</summary>
    public const string ToggleMfdRegionExpanded = "toggle_mfd_region_expanded";

    // ——— Вид: режим (дублируют хоткеи Alt+1/2/3, Ctrl+Alt+M)
    /// <summary>Установить Focus UI mode (hotkey). returns: text.</summary>
    public const string SetFocusModeUi = "set_focus_mode";
    /// <summary>Установить Balanced UI mode (hotkey). returns: text.</summary>
    public const string SetBalancedModeUi = "set_balanced_mode";
    /// <summary>Установить Power UI mode (hotkey). returns: text.</summary>
    public const string SetPowerModeUi = "set_power_mode";
    /// <summary>Циклически переключить UI mode (hotkey). returns: text.</summary>
    public const string CycleUiMode = "cycle_ui_mode";

    /// <summary>Открыть или закрыть палитру команд (как Ctrl+Q / пункт меню «Вид»). returns: text.</summary>
    public const string ToggleCommandPalette = "toggle_command_palette";

    /// <summary>Показать страницу «готовность окружения» во вторичном контуре (зона Mfd; ADR 0023). Разворачивает регион Mfd при необходимости. returns: text.</summary>
    public const string ShowEnvironmentReadinessPage = "show_environment_readiness_page";

    /// <summary>Перейти с страницы «готовность окружения» на первую другую разрешённую страницу вторичного контура. returns: text.</summary>
    public const string CloseEnvironmentReadinessPage = "close_environment_readiness_page";

    /// <summary>Активная страница вторичного контура оболочки: имя значения SecondaryShellPage (Chat, Terminal, …). Якорь на экране — пресет (v1 — колонка зоны Mfd). args: page:string; returns: text; example: {"page":"Chat"}.</summary>
    public const string SetSecondaryShellPage = "set_secondary_shell_page";

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

    /// <summary>Экспортировать текущий Markdown с развёрнутыми include-директивами. returns: text.</summary>
    public const string ExportExpandedMarkdown = "export_expanded_markdown";

    // ——— Вид: язык UI
    /// <summary>Установить язык UI. args: culture:string; returns: text; example: {"culture":"ru-RU"}.</summary>
    public const string SetUiLanguage = "set_ui_language";
    /// <summary>Сбросить язык UI к системному. returns: text.</summary>
    public const string ResetUiLanguageToSystem = "reset_ui_language_to_system";

    // ——— Меню: превью, настройки, справка
    /// <summary>Открыть отдельное окно превью (Markdown). returns: text.</summary>
    public const string OpenPreviewWindow = "open_preview_window";
    /// <summary>Открыть или активировать окно-хост зоны Mfd, если строка <c>presentation</c> / <c>zone_screen_layout</c> задаёт топологию с выносом Mfd (ADR 0017); иначе не выполняется. Отдельного пункта меню нет — источник истины раскладка. returns: text.</summary>
    public const string ToggleMfdHostWindow = "toggle_mfd_host_window";
    /// <summary>Открыть окно настроек. returns: text.</summary>
    public const string OpenSettings = "open_settings";
    /// <summary>Показать диалог «О программе». returns: text.</summary>
    public const string About = "about";

    // ——— Тулбар: показать панели / скрыть вывод сборки
    /// <summary>Развернуть регион Pfd (toolbar). returns: text.</summary>
    public const string ShowPfdRegionPanel = "show_pfd_region_panel";
    /// <summary>Явно показать панель вывода сборки (toolbar). returns: text.</summary>
    public const string ShowBuildOutputPanel = "show_build_output_panel";
    /// <summary>Развернуть регион Mfd и перейти на страницу Chat (toolbar). returns: text.</summary>
    public const string ShowChatPage = "show_chat_page";
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

    /// <summary>Кнопка «Собрать» в тулбаре: dotnet build в панель вывода (не structured build). returns: text.</summary>
    public const string BuildSolutionUi = "build_solution_ui";
}
