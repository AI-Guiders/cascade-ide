namespace CascadeIDE.Services.Presentation;

/// <summary>Правила v1 ADR 0017: когда колонку Mfd в главном окне можно перенести на отдельный <c>TopLevel</c>.</summary>
public static class PresentationLayoutAnalyzer
{
    /// <summary>
    /// Два дисплея: на первом — только PFD и forward (без Mfd), на втором — только Mfd.
    /// Типичный пресет <c>(PFD+Forward) (MFD)</c>, <c>(P+F) (M)</c>.
    /// </summary>
    public static bool IsDedicatedMfdSecondScreenPreset(IReadOnlyList<IReadOnlyList<PresentationAnchorKind>> screens)
    {
        if (screens.Count < 2)
            return false;

        var first = screens[0];
        var second = screens[1];
        if (second.Count != 1 || second[0] != PresentationAnchorKind.Mfd)
            return false;

        var hasPfd = ContainsAnchor(first, PresentationAnchorKind.Pfd);
        var hasFwd = ContainsAnchor(first, PresentationAnchorKind.Forward);
        var hasMfd = ContainsAnchor(first, PresentationAnchorKind.Mfd);
        return hasPfd && hasFwd && !hasMfd;
    }

    /// <summary>Три дисплея: по одному якорю — <c>(PFD) (Forward) (MFD)</c> (ADR 0017).</summary>
    public static bool IsTriplePfdForwardMfdPreset(IReadOnlyList<IReadOnlyList<PresentationAnchorKind>> screens)
    {
        if (screens.Count != 3)
            return false;

        return IsSingleAnchor(screens[0], PresentationAnchorKind.Pfd)
            && IsSingleAnchor(screens[1], PresentationAnchorKind.Forward)
            && IsSingleAnchor(screens[2], PresentationAnchorKind.Mfd);
    }

    /// <summary>
    /// Индекс группы в строке <c>presentation</c>, которой соответствует окно-хост MFD (сопоставляется с N-м дисплеем в порядке
    /// <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>). Иначе <c>false</c> — плейсмент без семантики.
    /// </summary>
    public static bool TryGetMfdHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorKind>> screens,
        out int index)
    {
        index = -1;
        if (IsDedicatedMfdSecondScreenPreset(screens))
        {
            index = 1;
            return true;
        }

        if (IsTriplePfdForwardMfdPreset(screens))
        {
            index = 2;
            return true;
        }

        return false;
    }

    private static bool IsSingleAnchor(IReadOnlyList<PresentationAnchorKind> screen, PresentationAnchorKind kind) =>
        screen.Count == 1 && screen[0] == kind;

    private static bool ContainsAnchor(IReadOnlyList<PresentationAnchorKind> screen, PresentationAnchorKind kind)
    {
        for (var i = 0; i < screen.Count; i++)
        {
            if (screen[i] == kind)
                return true;
        }

        return false;
    }
}
