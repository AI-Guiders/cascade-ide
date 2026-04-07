namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Стабильные id панелей для <c>workspace.toml</c> → привязка к якорям <see cref="AttentionZone"/> (forward/pfd/mfd; не канал EICAS).
/// Расширять по мере появления привязок в UI.
/// </summary>
public static class AttentionPanelIds
{
    public const string SolutionExplorer = "solution_explorer";
    public const string ChatPanel = "chat_panel";
    public const string Git = "git";
    public const string TerminalDock = "terminal_dock";
    public const string Editor = "editor";

    /// <summary>Полоса HUD над текстом редактора (слой внутри лобового, не отдельный якорь-колонка).</summary>
    public const string EditorHud = "editor_hud";
}
