namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Стабильные id поверхностей для ключа в <c>workspace.toml</c> → <see cref="AttentionZone"/> (ADR 0021).
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
