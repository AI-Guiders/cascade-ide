using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Cockpit.Composition.Shell;

/// <summary>
/// Композитор <b>оболочки</b> главного окна (ADR 0036 п.3): из intent + CDS policy + топологии хоста MFD
/// получает видимость колонок и ширину региона MFD в <c>MainGrid</c> (не дерево контролов, не данные каналов).
/// Полный кадр для хоста (колонки + инструменты слотов) — <c>MainWindowHostSurfaceCompositor</c> в <c>Cockpit/Composition/HostSurface</c>.
/// </summary>
public static class MainWindowShellSurfaceCompositor
{
    public static MainWindowShellSurfaceComposition Compose(in MainWindowShellSurfaceCompositionInput input)
    {
        if (input.EffectivePresentationTier == PresentationTierKind.Compact)
            return ComposeCompact(input);

        return ComposeCockpit(input);
    }

    private static MainWindowShellSurfaceComposition ComposeCompact(in MainWindowShellSurfaceCompositionInput input)
    {
        var presentation = input.DisplaySettings.Presentation;
        var placement = presentation.CompactIntercomPlacement?.Trim() ?? "side";
        var sideIntercom = !string.Equals(placement, "bottom", StringComparison.OrdinalIgnoreCase);
        var chatExpanded = input.IntentChatPanelExpanded && !input.SuppressMfdColumnForMfdHostWindow;
        var intercomAuxVisible = sideIntercom && chatExpanded;
        var intercomBottomVisible = !sideIntercom && chatExpanded;

        var intercomWidth = intercomAuxVisible
            ? Math.Max(
                input.CollapsedMfdWidthPixels,
                presentation.CompactAuxiliaryPanelWidthPx)
            : 0;

        var pfdRightVisible = input.IntentSolutionExplorerVisible
            && !input.SuppressPfdColumnForPfdHostWindow;
        var pfdRightWidth = pfdRightVisible
            ? CompactIdeLayoutMetrics.PfdRightColumnWidthPixels
            : 0;

        var rightChromeWidth = Math.Max(intercomWidth, pfdRightWidth);

        return new MainWindowShellSurfaceComposition(
            PfdSurfaceVisible: false,
            MfdSurfaceExpanded: input.IntentChatPanelExpanded,
            MfdColumnVisibleInMainGrid: false,
            MfdColumnPixelWidthInMainGrid: 0,
            CompactIdeLayout: true,
            IntercomAuxColumnVisible: intercomAuxVisible,
            IntercomAuxColumnPixelWidth: intercomWidth,
            IntercomBottomDockVisible: intercomBottomVisible,
            IntercomBottomDockHeightPx: presentation.CompactIntercomBottomDockHeightPx,
            PfdRightColumnVisible: pfdRightVisible,
            PfdRightColumnPixelWidth: pfdRightWidth,
            CompactRightChromeColumnVisible: intercomAuxVisible || pfdRightVisible,
            CompactRightChromeColumnPixelWidth: rightChromeWidth,
            MfdBottomDockHeightPx: presentation.CompactMfdBottomDockHeightPx);
    }

    private static MainWindowShellSurfaceComposition ComposeCockpit(in MainWindowShellSurfaceCompositionInput input)
    {
        var tier = input.EffectivePresentationTier;
        var presentationSpecifiesScreens = input.PresentationParse.IsSuccess && input.PresentationParse.Screens.Count > 0;
        var pfdRequiredOnMain = !presentationSpecifiesScreens
            || CockpitPresentationLayoutPolicy.RequiresPfdRegionInMainWindow(input.PresentationParse, tier);
        var pfdCoerced = CockpitPresentationLayoutPolicy.CoercePfdRegionExpanded(
            input.PresentationParse,
            tier,
            input.IntentSolutionExplorerVisible);
        var pfdVisible = pfdRequiredOnMain && pfdCoerced && !input.SuppressPfdColumnForPfdHostWindow;

        var mfdRequiredOnMain = !presentationSpecifiesScreens
            || CockpitPresentationLayoutPolicy.RequiresMfdRegionInMainWindow(input.PresentationParse, tier);
        var mfdExpanded = CockpitPresentationLayoutPolicy.CoerceMfdRegionExpanded(
            input.PresentationParse,
            tier,
            input.IntentChatPanelExpanded);

        var mfdColumnInMain = mfdRequiredOnMain && !input.SuppressMfdColumnForMfdHostWindow && mfdExpanded;

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

/// <summary>Вход композитора: intent пользователя, пресет, подавление колонок PFD/MFD в main при открытых хостах, числа ширин из UI-режима.</summary>
public readonly record struct MainWindowShellSurfaceCompositionInput(
    PresentationParseResult PresentationParse,
    bool IntentSolutionExplorerVisible,
    bool IntentChatPanelExpanded,
    bool SuppressPfdColumnForPfdHostWindow,
    bool SuppressMfdColumnForMfdHostWindow,
    int ExpandedMfdWidthPixels,
    int CollapsedMfdWidthPixels,
    DisplaySettings DisplaySettings,
    string SafetyLevel,
    PresentationTierKind EffectivePresentationTier = PresentationTierKind.Cockpit);

/// <summary>Результат: что отдать слою поверхности (привязки VM / code-behind) для колонок PFD/MFD.</summary>
public readonly record struct MainWindowShellSurfaceComposition(
    bool PfdSurfaceVisible,
    bool MfdSurfaceExpanded,
    bool MfdColumnVisibleInMainGrid,
    int MfdColumnPixelWidthInMainGrid,
    bool CompactIdeLayout = false,
    bool IntercomAuxColumnVisible = false,
    int IntercomAuxColumnPixelWidth = 0,
    bool IntercomBottomDockVisible = false,
    int IntercomBottomDockHeightPx = CompactIdeLayoutMetrics.IntercomBottomDockDefaultHeightPixels,
    bool PfdRightColumnVisible = false,
    int PfdRightColumnPixelWidth = 0,
    bool CompactRightChromeColumnVisible = false,
    int CompactRightChromeColumnPixelWidth = 0,
    int MfdBottomDockHeightPx = 220);
