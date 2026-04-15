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
    bool PfdRegionExpanded,
    bool BuildOutputVisible,
    bool TerminalVisible,
    bool MfdRegionExpanded,
    int EditorGroupCount,
    UiModeThemeSlot ThemeSlot,
    bool SelectTerminalTabWhenTerminalShown,
    bool InstrumentationDockVisible);

/// <summary>
/// Встроенные режимы UI: порядок в списке UI, раскладка при <c>ApplyUiModeLayout</c>, ширина региона Mfd.
/// Новый режим — новая запись в словаре (позже можно подменить загрузкой из JSON тем же типом).
/// </summary>
public static class UiModeLayoutRegistry
{
    /// <summary>Стабильный порядок пунктов «Режим интерфейса» и комбо.</summary>
    public static readonly IReadOnlyList<string> OrderedModeIds =
        ["Focus", "Editor", "Balanced", "Power", "AgentChat", "Debug"];

    private static readonly Dictionary<string, UiModeLayoutSpec> ByNormalizedMode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Focus"] = new UiModeLayoutSpec(
                PfdRegionExpanded: true,
                BuildOutputVisible: false,
                TerminalVisible: false,
                MfdRegionExpanded: true,
                EditorGroupCount: 1,
                ThemeSlot: UiModeThemeSlot.Dark,
                SelectTerminalTabWhenTerminalShown: false,
                InstrumentationDockVisible: true),
            // Три якоря PFD | Forward | MFD видны сразу (ADR 0021); «лёгкий» Editor — через capabilities/TOML, не через схлопывание колонок.
            ["Editor"] = new UiModeLayoutSpec(
                PfdRegionExpanded: true,
                BuildOutputVisible: false,
                TerminalVisible: false,
                MfdRegionExpanded: true,
                EditorGroupCount: 1,
                ThemeSlot: UiModeThemeSlot.Dark,
                SelectTerminalTabWhenTerminalShown: false,
                InstrumentationDockVisible: false),
            ["Balanced"] = new UiModeLayoutSpec(
                PfdRegionExpanded: true,
                BuildOutputVisible: true,
                TerminalVisible: true,
                MfdRegionExpanded: true,
                EditorGroupCount: 2,
                ThemeSlot: UiModeThemeSlot.CursorLike,
                SelectTerminalTabWhenTerminalShown: false,
                InstrumentationDockVisible: true),
            ["Power"] = new UiModeLayoutSpec(
                PfdRegionExpanded: true,
                BuildOutputVisible: true,
                TerminalVisible: true,
                MfdRegionExpanded: true,
                EditorGroupCount: 3,
                ThemeSlot: UiModeThemeSlot.PowerCockpit,
                SelectTerminalTabWhenTerminalShown: true,
                InstrumentationDockVisible: true),
            ["AgentChat"] = new UiModeLayoutSpec(
                PfdRegionExpanded: false,
                BuildOutputVisible: false,
                TerminalVisible: false,
                MfdRegionExpanded: true,
                EditorGroupCount: 1,
                ThemeSlot: UiModeThemeSlot.CursorLike,
                SelectTerminalTabWhenTerminalShown: false,
                InstrumentationDockVisible: true),
            ["Debug"] = new UiModeLayoutSpec(
                PfdRegionExpanded: true,
                BuildOutputVisible: false,
                TerminalVisible: false,
                MfdRegionExpanded: true,
                EditorGroupCount: 2,
                ThemeSlot: UiModeThemeSlot.Dark,
                SelectTerminalTabWhenTerminalShown: false,
                InstrumentationDockVisible: true),
        };

    /// <summary>Спека для режима; неизвестный режим трактуется как Balanced.</summary>
    public static UiModeLayoutSpec Get(string normalizedMode) =>
        ByNormalizedMode.TryGetValue(normalizedMode, out var spec)
            ? spec
            : ByNormalizedMode["Balanced"];

    /// <summary>Ширина развёрнутого региона Mfd в пикселях для нормализованного режима (с учётом <c>workspace.toml</c> через <see cref="UiWorkspaceLayoutRuntimeMetrics"/>).</summary>
    public static int GetMfdRegionExpandedWidthPixels(string normalizedMode) => normalizedMode switch
    {
        "Power" => UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedPowerWidthPixels,
        "AgentChat" => UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedAgentChatWidthPixels,
        _ => UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedDefaultWidthPixels,
    };
}
