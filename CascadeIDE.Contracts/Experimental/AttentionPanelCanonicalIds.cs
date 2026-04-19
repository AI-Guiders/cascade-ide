namespace CascadeIDE.Contracts.Experimental;

/// <summary>
/// Канонические id панелей shell для <c>workspace.toml</c> / <see cref="Experimental.Capabilities.UiSurfaceCapabilityDescriptor.HostAttentionPanelId"/>.
/// ADR 0025; совпадают с <c>AttentionPanelIds</c> в приложении.
/// </summary>
[ApiStability(ApiStability.Experimental)]
public static class AttentionPanelCanonicalIds
{
    public const string SolutionExplorer = "solution_explorer";
    public const string ChatPanel = "chat_panel";
    public const string Git = "git";
    public const string Terminal = "terminal";
    public const string Editor = "editor";
    public const string EditorHud = "editor_hud";

    /// <summary>Известные id в стабильном порядке (расширять вместе с <c>AttentionZonePanelRuntime</c>).</summary>
    public static readonly string[] All =
    [
        SolutionExplorer,
        ChatPanel,
        Git,
        Terminal,
        Editor,
        EditorHud
    ];

    /// <summary>Строгое совпадение с одним из канонических id панели.</summary>
    public static bool IsKnownPanelId(string? value) =>
        value is SolutionExplorer or ChatPanel or Git or Terminal or Editor or EditorHud;
}
