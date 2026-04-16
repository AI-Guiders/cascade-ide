using CascadeIDE.Contracts.Experimental;

namespace CascadeIDE.Features.UiChrome;

/// <summary>
/// Стабильные id панелей для <c>workspace.toml</c> → привязка к якорям <see cref="AttentionZone"/> (forward/pfd/mfd; не канал EICAS).
/// Расширять по мере появления привязок в UI и в <see cref="AttentionPanelCanonicalIds"/>.
/// </summary>
public static class AttentionPanelIds
{
    public const string SolutionExplorer = AttentionPanelCanonicalIds.SolutionExplorer;
    public const string ChatPanel = AttentionPanelCanonicalIds.ChatPanel;
    public const string Git = AttentionPanelCanonicalIds.Git;
    public const string Terminal = AttentionPanelCanonicalIds.Terminal;
    public const string Editor = AttentionPanelCanonicalIds.Editor;

    /// <summary>Полоса HUD над текстом редактора (слой внутри лобового, не отдельный якорь-колонка).</summary>
    public const string EditorHud = AttentionPanelCanonicalIds.EditorHud;
}