namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Единая точка правил «пресет <c>presentation</c> не даёт сломать кабину»: что должно остаться видимым
/// на первом экране (главное окно), независимо от UI-режима, MCP и меню (ADR 0017).
/// </summary>
public static class PresentationLayoutAuthority
{
    /// <summary>На первом экране есть якорь PFD — колонка обозревателя в главном окне не может быть скрыта.</summary>
    public static bool RequiresVisiblePfdColumn(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Pfd);

    /// <summary>На первом экране есть якорь MFD — правая колонка зоны M в главном окне не может быть свёрнута в ноль (якорь привязан к первому дисплею).</summary>
    public static bool RequiresExpandedChatColumnForMainWindow(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Mfd);

    /// <summary>На первом экране есть якорь Forward — центральная зона редактора обязана иметь место (см. <c>MainGrid</c>); v1 нет отдельного «выкл. forward», метод для симметрии и будущих проверок.</summary>
    public static bool RequiresForwardOnFirstScreen(PresentationParseResult parse) =>
        FirstScreenContains(parse, PresentationAnchorKind.Forward);

    public static bool CoerceSolutionExplorerVisible(PresentationParseResult parse, bool desired) =>
        RequiresVisiblePfdColumn(parse) ? true : desired;

    public static bool CoerceChatPanelExpanded(PresentationParseResult parse, bool desired) =>
        RequiresExpandedChatColumnForMainWindow(parse) ? true : desired;

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
