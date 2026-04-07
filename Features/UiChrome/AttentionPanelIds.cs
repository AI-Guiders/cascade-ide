namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Стабильные id панелей для <c>workspace.toml</c> → привязка к якорям <see cref="AttentionZone"/> (frontal/pfd/mfd; не канал EICAS).
/// Расширять по мере появления привязок в UI.
/// </summary>
public static class AttentionPanelIds
{
    public const string SolutionExplorer = "solution_explorer";
    public const string ChatPanel = "chat_panel";
    public const string Git = "git";
    public const string TerminalDock = "terminal_dock";
    public const string Editor = "editor";
}
