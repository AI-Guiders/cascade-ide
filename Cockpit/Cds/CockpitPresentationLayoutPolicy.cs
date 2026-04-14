using CascadeIDE.Services.Presentation;

namespace CascadeIDE.Cockpit.Cds;

/// <summary>
/// CDS: инварианты раскладки по <c>presentation</c> (первый экран) — канон правил для shell (ADR 0036, ADR 0046).
/// </summary>
public static class CockpitPresentationLayoutPolicy
{
    /// <summary>На первом экране есть якорь PFD — колонка обозревателя в главном окне не может быть скрыта.</summary>
    public static bool RequiresVisiblePfdColumn(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Pfd);

    /// <summary>На первом экране есть якорь MFD — правая колонка зоны M в главном окне не может быть свёрнута в ноль.</summary>
    public static bool RequiresExpandedChatColumnForMainWindow(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Mfd);

    /// <summary>На первом экране есть якорь Forward — для симметрии и будущих проверок (v1 нет отдельного «выкл. forward»).</summary>
    public static bool RequiresForwardOnFirstScreen(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Forward);

    public static bool CoerceSolutionExplorerVisible(PresentationParseResult parse, bool desired) =>
        RequiresVisiblePfdColumn(parse) ? true : desired;

    public static bool CoerceChatPanelExpanded(PresentationParseResult parse, bool desired) =>
        RequiresExpandedChatColumnForMainWindow(parse) ? true : desired;

    /// <summary>Флаги «якорь обязателен на первом экране» для сериализации в <see cref="CockpitSurfaceZones"/>.</summary>
    public static CockpitPresentationLayoutInvariants InvariantsFromPresentation(PresentationParseResult parse) =>
        new(
            RequiresVisiblePfdColumn(parse),
            RequiresForwardOnFirstScreen(parse),
            RequiresExpandedChatColumnForMainWindow(parse));

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
