using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Cockpit.Composition.Shell;

/// <summary>
/// Композитор поверхности главного окна (ADR 0036 п.3): из intent + CDS policy + топологии хоста MFD
/// получает видимость колонок и ширину региона MFD в <c>MainGrid</c> (не дерево контролов, не данные каналов).
/// </summary>
public static class MainWindowShellSurfaceCompositor
{
    public static MainWindowShellSurfaceComposition Compose(in MainWindowShellSurfaceCompositionInput input)
    {
        var pfdVisible = CockpitPresentationLayoutPolicy.CoerceSolutionExplorerVisible(
            input.PresentationParse,
            input.IntentSolutionExplorerVisible);

        var mfdExpanded = CockpitPresentationLayoutPolicy.CoerceChatPanelExpanded(
            input.PresentationParse,
            input.IntentChatPanelExpanded);

        var mfdColumnInMain = !input.SuppressMfdColumnForMfdHostWindow && mfdExpanded;

        var width = mfdColumnInMain
            ? (mfdExpanded ? input.ExpandedMfdWidthPixels : input.CollapsedMfdWidthPixels)
            : 0;

        return new MainWindowShellSurfaceComposition(
            PfdSurfaceVisible: pfdVisible,
            MfdSurfaceExpanded: mfdExpanded,
            MfdColumnVisibleInMainGrid: mfdColumnInMain,
            MfdColumnPixelWidthInMainGrid: width);
    }
}

/// <summary>Вход композитора: intent пользователя, пресет, подавление колонки MFD в main при открытом хосте, числа ширин из UI-режима.</summary>
public readonly record struct MainWindowShellSurfaceCompositionInput(
    PresentationParseResult PresentationParse,
    bool IntentSolutionExplorerVisible,
    bool IntentChatPanelExpanded,
    bool SuppressMfdColumnForMfdHostWindow,
    int ExpandedMfdWidthPixels,
    int CollapsedMfdWidthPixels);

/// <summary>Результат: что отдать слою поверхности (привязки VM / code-behind) для колонок PFD/MFD.</summary>
public readonly record struct MainWindowShellSurfaceComposition(
    bool PfdSurfaceVisible,
    bool MfdSurfaceExpanded,
    bool MfdColumnVisibleInMainGrid,
    int MfdColumnPixelWidthInMainGrid);
