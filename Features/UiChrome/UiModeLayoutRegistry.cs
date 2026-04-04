namespace CascadeIDE.Features.UiChrome;

/// <summary>Какую встроенную тему подставить при входе в режим.</summary>
public enum UiModeThemeSlot
{
    CursorLike,
    Dark,
    PowerCockpit,
}

/// <summary>Снимок раскладки панелей и темы для одного нормализованного режима (<see cref="UiChromeViewModel.NormalizeUiMode"/>).</summary>
public sealed record UiModeLayoutSpec(
    bool SolutionExplorerVisible,
    bool BuildOutputVisible,
    bool TerminalVisible,
    bool ChatPanelExpanded,
    int EditorGroupCount,
    UiModeThemeSlot ThemeSlot,
    bool SelectTerminalTabWhenTerminalShown);

/// <summary>
/// Встроенные режимы UI: порядок в списке UI, раскладка при <c>ApplyUiModeLayout</c>, ширина чата.
/// Новый режим — новая запись в словаре (позже можно подменить загрузкой из JSON тем же типом).
/// </summary>
public static class UiModeLayoutRegistry
{
    /// <summary>Стабильный порядок пунктов «Режим интерфейса» и комбо.</summary>
    public static readonly IReadOnlyList<string> OrderedModeIds =
        ["Focus", "Balanced", "Power", "AgentChat", "Debug"];

    private static readonly Dictionary<string, UiModeLayoutSpec> ByNormalizedMode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Focus"] = new UiModeLayoutSpec(
                SolutionExplorerVisible: true,
                BuildOutputVisible: false,
                TerminalVisible: false,
                ChatPanelExpanded: true,
                EditorGroupCount: 1,
                ThemeSlot: UiModeThemeSlot.Dark,
                SelectTerminalTabWhenTerminalShown: false),
            ["Balanced"] = new UiModeLayoutSpec(
                SolutionExplorerVisible: true,
                BuildOutputVisible: true,
                TerminalVisible: true,
                ChatPanelExpanded: true,
                EditorGroupCount: 2,
                ThemeSlot: UiModeThemeSlot.CursorLike,
                SelectTerminalTabWhenTerminalShown: false),
            ["Power"] = new UiModeLayoutSpec(
                SolutionExplorerVisible: true,
                BuildOutputVisible: true,
                TerminalVisible: true,
                ChatPanelExpanded: true,
                EditorGroupCount: 3,
                ThemeSlot: UiModeThemeSlot.PowerCockpit,
                SelectTerminalTabWhenTerminalShown: true),
            ["AgentChat"] = new UiModeLayoutSpec(
                SolutionExplorerVisible: false,
                BuildOutputVisible: false,
                TerminalVisible: false,
                ChatPanelExpanded: true,
                EditorGroupCount: 1,
                ThemeSlot: UiModeThemeSlot.CursorLike,
                SelectTerminalTabWhenTerminalShown: false),
            ["Debug"] = new UiModeLayoutSpec(
                SolutionExplorerVisible: true,
                BuildOutputVisible: false,
                TerminalVisible: false,
                ChatPanelExpanded: false,
                EditorGroupCount: 2,
                ThemeSlot: UiModeThemeSlot.Dark,
                SelectTerminalTabWhenTerminalShown: false),
        };

    /// <summary>Спека для режима; неизвестный режим трактуется как Balanced.</summary>
    public static UiModeLayoutSpec Get(string normalizedMode) =>
        ByNormalizedMode.TryGetValue(normalizedMode, out var spec)
            ? spec
            : ByNormalizedMode["Balanced"];

    /// <summary>Ширина развёрнутой колонки чата в пикселях для нормализованного режима.</summary>
    public static int GetChatPanelExpandedWidthPixels(string normalizedMode) => normalizedMode switch
    {
        "Power" => UiModeLayoutDimensions.ChatPanelExpandedPowerWidthPixels,
        "AgentChat" => UiModeLayoutDimensions.ChatPanelExpandedAgentChatWidthPixels,
        _ => UiModeLayoutDimensions.ChatPanelExpandedDefaultWidthPixels,
    };
}
