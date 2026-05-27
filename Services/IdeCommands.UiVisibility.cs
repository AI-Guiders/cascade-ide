namespace CascadeIDE.Services;

/// <summary>Фокус, снимок окна, видимость панелей и режим UI (ide_execute_command).</summary>
public static partial class IdeCommands
{
    /// <summary>Передать фокус в редактор (чтобы клавиши/ввод шли в него). returns: text.</summary>
    public const string FocusEditor = "focus_editor";
    /// <summary>Снимок окон IDE в PNG (по умолчанию главное окно; при scope=all — все top-level, в т.ч. окно-хост Mfd и прочие). args: scope?:string, workspace_path?:string, output_path?:string; returns: json. example: {"scope":"all","workspace_path":"D:\\\\tmp\\\\ws","output_path":".cascade-ide/window-{n}.png"}.</summary>
    public const string CaptureWindow = "capture_window";
    /// <summary>Как меню «Вид → Терминал» (переключатель). returns: text.</summary>
    public const string ToggleTerminal = "toggle_terminal";
    /// <summary>Сплиттеры рабочей области: переключить ON GND / IN AIR (мелодия tol, лампа TOL в task cockpit). returns: text.</summary>
    public const string ToggleWorkspaceSplittersLock = "toggle_workspace_splitters_lock";
    /// <summary>Как меню «Вид → Вывод сборки». returns: text.</summary>
    public const string ToggleBuildOutput = "toggle_build_output";
    /// <summary>Переключить развёрнут/свёрнут регион Pfd (как меню «Вид → Карта намерений (PFD)»). returns: text.</summary>
    public const string TogglePfdRegionExpanded = "toggle_pfd_region_expanded";

    /// <summary>Карта намерений: цикл вида list → graph → both (палитра; быстрый путь — Ctrl+K → S → P). returns: text.</summary>
    public const string CycleCodeNavigationMapPresentation = "cycle_code_navigation_map_presentation";

    /// <summary>Карта намерений: переключить уровень file ↔ controlFlow (Ctrl+K → S → F). returns: text.</summary>
    public const string CycleCodeNavigationMapLevel = "cycle_code_navigation_map_level";

    /// <summary>Карта намерений: цикл детализации glance → normal → inspect (Ctrl+K → S → D). returns: text.</summary>
    public const string CycleCodeNavigationMapDetailLevel = "cycle_code_navigation_map_detail_level";

    /// <summary>Карта намерений: цикл укладки related-files radial → top_down → bottom_up. returns: text.</summary>
    public const string CycleCodeNavigationMapRelatedGraphLayout = "cycle_code_navigation_map_related_graph_layout";
    /// <summary>Явно показать/скрыть терминал (без переключения). args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetTerminalVisible = "set_terminal_visible";
    /// <summary>Явно показать/скрыть журнал сборки. args: visible:boolean; returns: text; example: {"visible":true}.</summary>
    public const string SetBuildOutputVisible = "set_build_output_visible";
    /// <summary>Режим UI (как меню «Вид → Режим интерфейса»). args: mode:string; returns: text; example: {"mode":"Flight"}.</summary>
    public const string SetUiMode = "set_ui_mode";
}
