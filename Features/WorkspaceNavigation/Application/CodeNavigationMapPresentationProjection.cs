using CascadeIDE.Models;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Проекция UI для панели карты намерений (PFD): видимость list/graph и вспомогательные строки без привязки к ViewModel.
/// </summary>
public static class CodeNavigationMapPresentationProjection
{
    /// <summary>Список related на странице MFD (вкладка RelatedFiles), не в колонке PFD.</summary>
    public const bool ShowCodeNavigationMapListOnPfd = false;

    public static bool ShowCodeNavigationMapList(string presentationView) =>
        CodeNavigationMapSettings.ViewWantsList(presentationView);

    public static bool ShowCodeNavigationMapGraph(string presentationView) =>
        CodeNavigationMapSettings.ViewWantsGraph(presentationView);

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
