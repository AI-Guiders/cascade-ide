using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// CDS: инварианты раскладки по <c>presentation</c> (первый экран) — канон правил для shell (ADR 0036, ADR 0046).
/// </summary>
public static class CockpitPresentationLayoutPolicy
{
    /// <summary>На первом экране есть якорь PFD — регион Pfd в главном окне не может быть свёрнут в ноль.</summary>
    public static bool RequiresPfdRegionInMainWindow(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Pfd);

    /// <summary>На первом экране есть якорь MFD — регион Mfd в главном окне не может быть свёрнут в ноль.</summary>
    public static bool RequiresMfdRegionInMainWindow(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Mfd);

    /// <summary>На первом экране есть якорь Forward — для симметрии и будущих проверок (v1 нет отдельного «выкл. forward»).</summary>
    public static bool RequiresForwardOnFirstScreen(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Forward);

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

    private static bool FirstScreenContains(PresentationParseResult parse, PresentationAnchorKind kind)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return false;

        var first = parse.Screens[0];
        for (var i = 0; i < first.Count; i++)
        {
            if (first[i].Kind == kind)
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
