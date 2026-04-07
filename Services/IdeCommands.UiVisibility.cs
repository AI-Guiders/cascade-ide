namespace CascadeIDE.Services;

/// <summary>Фокус, снимок окна, видимость панелей и режим UI (ide_execute_command).</summary>
public static partial class IdeCommands
{
    /// <summary>Передать фокус в редактор (чтобы клавиши/ввод шли в него). returns: text.</summary>
    public const string FocusEditor = "focus_editor";
    /// <summary>Снимок окон IDE в PNG (по умолчанию главное окно; при scope=all — все top-level, в т.ч. вспомогательные). args: scope?:string, workspace_path?:string, output_path?:string; returns: json. example: {"scope":"all","workspace_path":"D:\\\\tmp\\\\ws","output_path":".cascade-ide/window-{n}.png"}.</summary>
    public const string CaptureWindow = "capture_window";
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
}
