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
/// Встроенный режим UI: порядок в списке UI, раскладка при <c>ApplyUiModeLayout</c>, ширина региона Mfd.
/// Шипнутый бандл <c>UiModes/index.toml</c> задаёт id (в т.ч. <c>Flight</c>, <c>Editor</c>); неизвестный id в спеке — как Flight.
/// </summary>
public static class UiModeLayoutRegistry
{
    /// <summary>Стабильный порядок пункта «Режим интерфейса» и комбо.</summary>
    public static readonly IReadOnlyList<string> OrderedModeIds = ["Flight"];

    private static readonly Dictionary<string, UiModeLayoutSpec> ByNormalizedMode =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Базовая «рабочая» раскладка (раньше Balanced): терминал/сборка, два редактора, MFD.
            ["Flight"] = new UiModeLayoutSpec(
                PfdRegionExpanded: true,
                BuildOutputVisible: true,
                TerminalVisible: true,
                MfdRegionExpanded: true,
                EditorGroupCount: 2,
                ThemeSlot: UiModeThemeSlot.CursorLike,
                SelectTerminalTabWhenTerminalShown: false,
                InstrumentationDockVisible: true),
        };

    /// <summary>Спека для режима; неизвестный режим — как Flight.</summary>
    public static UiModeLayoutSpec Get(string normalizedMode) =>
        ByNormalizedMode.TryGetValue(normalizedMode, out var spec)
            ? spec
            : ByNormalizedMode["Flight"];

    /// <summary>Ширина развёрнутого региона Mfd в пикселях для нормализованного режима (с учётом <c>workspace.toml</c> через <see cref="UiWorkspaceLayoutRuntimeMetrics"/>).</summary>
    public static int GetMfdRegionExpandedWidthPixels(string normalizedMode) => normalizedMode switch
    {
        _ => UiWorkspaceLayoutRuntimeMetrics.MfdRegionExpandedDefaultWidthPixels,
    };
}
