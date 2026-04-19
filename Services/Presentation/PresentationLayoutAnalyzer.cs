namespace CascadeIDE.Services.Presentation;

/// <summary>Правила v1 ADR 0017: когда колонку Mfd в главном окне можно перенести на отдельный <c>TopLevel</c>.</summary>
public static class PresentationLayoutAnalyzer
{
    /// <summary>
    /// Два дисплея: на первом — только PFD и forward (без Mfd), на втором — только Mfd.
    /// Типичный пресет <c>(PFD+Forward) (MFD)</c>, <c>(P+F) (M)</c>.
    /// </summary>
    public static bool IsDedicatedMfdSecondScreenPreset(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens)
    {
        if (screens.Count < 2)
            return false;

        var first = screens[0];
        var second = screens[1];
        if (second.Count != 1 || second[0].Kind != PresentationAnchorKind.Mfd)
            return false;

        var hasPfd = ContainsAnchor(first, PresentationAnchorKind.Pfd);
        var hasFwd = ContainsAnchor(first, PresentationAnchorKind.Forward);
        var hasMfd = ContainsAnchor(first, PresentationAnchorKind.Mfd);
        return hasPfd && hasFwd && !hasMfd;
    }

    /// <summary>
    /// Первый экран в строке объединяет PFD и Forward как <c>(xP+yF)</c> (веса или равные доли) — главное окно
    /// должно занимать рабочую область дисплея (максимизация при старте), а не дефолт 1000×600.
    /// </summary>
    public static bool IsPfdForwardCombinedOnFirstScreen(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens)
    {
        if (screens.Count == 0)
            return false;

        var first = screens[0];
        return ContainsAnchor(first, PresentationAnchorKind.Pfd)
            && ContainsAnchor(first, PresentationAnchorKind.Forward);
    }

    /// <summary>
    /// Главное окно при старте разворачиваем на рабочую область дисплея (не дефолт 1000×600):
    /// на первом экране есть и PFD, и Forward — <c>(xP+yF)</c>, <c>(xP+yF+zM)</c> в одной группе и т.п.;
    /// либо три дисплея <c>(P)(F)(M)</c> — первое окно только под PFD на первом мониторе (ADR 0017).
    /// Веса <c>x</c>/<c>y</c> меняют только доли колонок; условие по составу якорей не зависит от чисел.
    /// </summary>
    public static bool ShouldMaximizeMainWindowAtStartup(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens) =>
        IsPfdForwardCombinedOnFirstScreen(screens) || IsTripleOneAnchorPerZonePreset(screens);

    /// <summary>Три дисплея: по одному якорю — <c>(PFD) (Forward) (MFD)</c> в этом порядке (ADR 0017).</summary>
    public static bool IsTriplePfdForwardMfdPreset(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens)
    {
        if (screens.Count != 3)
            return false;

        return IsSingleAnchor(screens[0], PresentationAnchorKind.Pfd)
            && IsSingleAnchor(screens[1], PresentationAnchorKind.Forward)
            && IsSingleAnchor(screens[2], PresentationAnchorKind.Mfd);
    }

    /// <summary>
    /// Три дисплея: ровно по одному якорю на экран, набор <c>P</c>/<c>F</c>/<c>M</c> без повторов (любой порядок групп в строке — ADR 0017).
    /// </summary>
    public static bool IsTripleOneAnchorPerZonePreset(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens)
    {
        if (screens.Count != 3)
            return false;

        PresentationAnchorKind? a0 = SingleAnchorKind(screens[0]);
        PresentationAnchorKind? a1 = SingleAnchorKind(screens[1]);
        PresentationAnchorKind? a2 = SingleAnchorKind(screens[2]);
        if (a0 is null || a1 is null || a2 is null)
            return false;

        return a0 != a1 && a1 != a2 && a0 != a2
            && HasAllZoneKinds(a0.Value, a1.Value, a2.Value);
    }

    /// <summary>
    /// Индекс группы в строке <c>presentation</c>, которой соответствует окно-хост MFD (сопоставляется с N-м дисплеем в порядке
    /// <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>). Иначе <c>false</c> — плейсмент без семантики.
    /// </summary>
    public static bool TryGetMfdHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index)
    {
        index = -1;
        if (IsDedicatedMfdSecondScreenPreset(screens))
        {
            index = 1;
            return true;
        }

        if (TryGetSingleAnchorScreenIndex(screens, PresentationAnchorKind.Mfd, out index))
            return true;

        return false;
    }

    /// <summary>Индекс экрана с единственным якорем <c>P</c> в тройном пресете <c>(…) (…) (…)</c>.</summary>
    public static bool TryGetPfdHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index) =>
        TryGetSingleAnchorScreenIndex(screens, PresentationAnchorKind.Pfd, out index);

    private static bool TryGetSingleAnchorScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        PresentationAnchorKind kind,
        out int index)
    {
        index = -1;
        if (!IsTripleOneAnchorPerZonePreset(screens))
            return false;

        for (var i = 0; i < screens.Count; i++)
        {
            if (screens[i].Count != 1)
                continue;
            if (screens[i][0].Kind != kind)
                continue;
            index = i;
            return true;
        }

        return false;
    }

    private static PresentationAnchorKind? SingleAnchorKind(IReadOnlyList<PresentationAnchorSlot> screen) =>
        screen.Count == 1 ? screen[0].Kind : null;

    private static bool HasAllZoneKinds(PresentationAnchorKind a, PresentationAnchorKind b, PresentationAnchorKind c)
    {
        var mask = 0;
        void Add(PresentationAnchorKind k) =>
            mask |= k switch
            {
                PresentationAnchorKind.Pfd => 1,
                PresentationAnchorKind.Forward => 2,
                PresentationAnchorKind.Mfd => 4,
                _ => 0
            };
        Add(a);
        Add(b);
        Add(c);
        return mask == 7;
    }

    private static bool IsSingleAnchor(IReadOnlyList<PresentationAnchorSlot> screen, PresentationAnchorKind kind) =>
        screen.Count == 1 && screen[0].Kind == kind;

    private static bool ContainsAnchor(IReadOnlyList<PresentationAnchorSlot> screen, PresentationAnchorKind kind)
    {
        for (var i = 0; i < screen.Count; i++)
        {
            if (screen[i].Kind == kind)
                return true;
        }

        return false;
    }
}
