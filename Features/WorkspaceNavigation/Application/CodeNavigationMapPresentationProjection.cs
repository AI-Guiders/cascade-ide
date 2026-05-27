using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Проекция UI для панели карты намерений (PFD): видимость list/graph и вспомогательные строки без привязки к ViewModel.
/// </summary>
[PresentationProjection]
public static class CodeNavigationMapPresentationProjection
{
    private static readonly string[] PresentationViewCycleOrder = ["list", "graph", "both"];

    /// <summary>Список related на странице MFD (вкладка RelatedFiles), не в колонке PFD.</summary>
    public const bool ShowCodeNavigationMapListOnPfd = false;

    /// <summary>Строка сводки настроек карты для HUD (без ComboBox).</summary>
    public static string SettingsSummaryLine(
        string presentationView,
        string mapLevel,
        string detailLevelRaw,
        string relatedGraphLayoutRaw,
        string controlFlowMainAxisRaw) =>
        $"Вид: {presentationView} · уровень: {mapLevel} · укладка: {CodeNavigationMapRelatedGraphLayoutKind.Normalize(relatedGraphLayoutRaw)} · CF ось: {CodeNavigationMapControlFlowMainAxisKind.Normalize(controlFlowMainAxisRaw)} · детализация: {detailLevelRaw.Trim()} · палитра / MCP";

    /// <summary>Цикл укладки related-files: radial → top_down → bottom_up.</summary>
    public static string NextRelatedGraphLayoutAfter(string current) =>
        CodeNavigationMapRelatedGraphLayoutKind.Normalize(current) switch
        {
            CodeNavigationMapRelatedGraphLayoutKind.Radial => CodeNavigationMapRelatedGraphLayoutKind.TopDown,
            CodeNavigationMapRelatedGraphLayoutKind.TopDown => CodeNavigationMapRelatedGraphLayoutKind.BottomUp,
            _ => CodeNavigationMapRelatedGraphLayoutKind.Radial
        };

    /// <summary>Следующий <see cref="CodeNavigationMapSettings.NormalizeView"/> после текущего: list → graph → both → list.</summary>
    public static string NextPresentationViewAfter(string currentPresentationView)
    {
        var cur = CodeNavigationMapSettings.NormalizeView(currentPresentationView);
        var i = Array.IndexOf(PresentationViewCycleOrder, cur);
        if (i < 0)
            i = 0;
        return PresentationViewCycleOrder[(i + 1) % PresentationViewCycleOrder.Length];
    }

    /// <summary>Следующий нормализованный depth: file ↔ controlFlow.</summary>
    public static string ToggledMapLevel(string currentDepth)
    {
        var n = CodeNavigationMapLevelKind.Normalize(currentDepth);
        return string.Equals(n, CodeNavigationMapLevelKind.File, StringComparison.Ordinal)
            ? CodeNavigationMapLevelKind.ControlFlow
            : CodeNavigationMapLevelKind.File;
    }

    /// <summary>Цикл детализации glance → normal → inspect → glance для <c>[code_navigation_map]</c>.</summary>
    public static (CodeNavigationMapDetailLevel Detail, string TomlDetailLevel) NextDetailCycle(CodeNavigationMapDetailLevel currentNormalized)
    {
        var next = currentNormalized switch
        {
            CodeNavigationMapDetailLevel.Glance => CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapDetailLevel.Normal => CodeNavigationMapDetailLevel.Inspect,
            CodeNavigationMapDetailLevel.Inspect => CodeNavigationMapDetailLevel.Glance,
            _ => CodeNavigationMapDetailLevel.Normal
        };
        var toml = next switch
        {
            CodeNavigationMapDetailLevel.Glance => "glance",
            CodeNavigationMapDetailLevel.Normal => "normal",
            CodeNavigationMapDetailLevel.Inspect => "inspect",
            _ => "normal"
        };
        return (next, toml);
    }

    public static bool ShowCodeNavigationMapList(string presentationView) =>
        CodeNavigationMapSettings.ViewWantsList(presentationView);

    /// <summary>
    /// Граф на PFD: для <c>controlFlow</c> всегда виден (основной продукт уровня), для <c>file</c> — по <c>view</c>.
    /// </summary>
    public static bool ShowCodeNavigationMapGraph(string presentationView, string mapLevel) =>
        string.Equals(
            CodeNavigationMapLevelKind.Normalize(mapLevel),
            CodeNavigationMapLevelKind.ControlFlow,
            StringComparison.Ordinal)
        || CodeNavigationMapSettings.ViewWantsGraph(presentationView);

    /// <summary>Нужна ли нижняя строка под список на PFD (политика: список только в MFD — всегда false).</summary>
    public static bool ListAreaRowUsesStar(bool showList, bool showListOnPfd) => showList && showListOnPfd;

    public static bool ShowCodeNavigationMapGraphClickHint(bool showGraph, string codeNavigationMapLevel, string presentationView)
    {
        if (!showGraph)
            return false;
        if (CodeNavigationMapSettings.NormalizeView(presentationView) == "list")
            return false;
        return string.Equals(
            CodeNavigationMapLevelKind.Normalize(codeNavigationMapLevel),
            CodeNavigationMapLevelKind.File,
            StringComparison.OrdinalIgnoreCase);
    }

    public static string WorkspaceNavigationMapRelatedBadge(int relatedCount) => relatedCount switch
    {
        0 => "",
        1 => "1 связь",
        _ => $"{relatedCount} связей"
    };

    public static bool WorkspaceNavigationMapHasRelated(int relatedCount, int? graphSceneNodeCount) =>
        relatedCount > 0 || graphSceneNodeCount > 1;
}
