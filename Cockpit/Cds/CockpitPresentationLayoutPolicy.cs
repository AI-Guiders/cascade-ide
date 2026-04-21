using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// CDS: инварианты раскладки по <c>presentation</c> (первый экран) — канон правил для shell (ADR 0036, ADR 0046).
/// </summary>
public static class CockpitPresentationLayoutPolicy
{
    /// <summary>
    /// На экране, который в <c>presentation</c> отдан под главное окно (лобовое), есть якорь PFD — регион Pfd в главном окне не может быть свёрнут в ноль.
    /// Для <c>(xP+yM)(F)</c> якоря P/M на экране сплита — в main не требуются (ADR 0017).
    /// </summary>
    public static bool RequiresPfdRegionInMainWindow(PresentationParseResult parse) =>
        MainWindowPresentationScreenContains(parse, PresentationAnchorKind.Pfd);

    /// <summary>См. <see cref="RequiresPfdRegionInMainWindow"/> — для MFD в главном окне.</summary>
    public static bool RequiresMfdRegionInMainWindow(PresentationParseResult parse) =>
        MainWindowPresentationScreenContains(parse, PresentationAnchorKind.Mfd);

    /// <summary>На экране главного окна в строке <c>presentation</c> есть якорь Forward (симметрия и будущие проверки).</summary>
    public static bool RequiresForwardOnFirstScreen(PresentationParseResult parse) =>
        MainWindowPresentationScreenContains(parse, PresentationAnchorKind.Forward);

    public static bool CoercePfdRegionExpanded(PresentationParseResult parse, bool desired) =>
        RequiresPfdRegionInMainWindow(parse) ? true : desired;

    public static bool CoerceMfdRegionExpanded(PresentationParseResult parse, bool desired) =>
        RequiresMfdRegionInMainWindow(parse) ? true : desired;

    /// <summary>Флаги «якорь присутствует в строке <c>presentation</c>» для сериализации в <see cref="CockpitSurfaceZones"/>.</summary>
    public static CockpitPresentationLayoutInvariants InvariantsFromPresentation(PresentationParseResult parse) =>
        new(
            AnyScreenContains(parse, PresentationAnchorKind.Pfd),
            AnyScreenContains(parse, PresentationAnchorKind.Forward),
            AnyScreenContains(parse, PresentationAnchorKind.Mfd));

    private static bool AnyScreenContains(PresentationParseResult parse, PresentationAnchorKind kind)
    {
        if (!parse.IsSuccess)
            return false;

        for (var s = 0; s < parse.Screens.Count; s++)
        {
            var screen = parse.Screens[s];
            for (var i = 0; i < screen.Count; i++)
            {
                if (screen[i].Kind == kind)
                    return true;
            }
        }

        return false;
    }

    private static bool MainWindowPresentationScreenContains(PresentationParseResult parse, PresentationAnchorKind kind)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return false;

        var idx = PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(parse);
        if ((uint)idx >= (uint)parse.Screens.Count)
            return false;

        var screen = parse.Screens[idx];
        for (var i = 0; i < screen.Count; i++)
        {
            if (screen[i].Kind == kind)
                return true;
        }

        return false;
    }
}

/// <summary>Снимок инвариантов первого экрана для CDS <c>zones</c>.</summary>
public readonly record struct CockpitPresentationLayoutInvariants(
    bool PfdRequiredByPresentation,
    bool ForwardRequiredByPresentation,
    bool MfdRequiredByPresentation);
