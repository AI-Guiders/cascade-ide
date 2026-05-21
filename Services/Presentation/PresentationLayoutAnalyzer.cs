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
    /// либо три дисплея <c>(P)(F)(M)</c> — первое окно только под PFD на первом мониторе (ADR 0017);
    /// либо два экрана <c>(xP+yM)(F)</c> / <c>(F)(xP+yM)</c> — на главном только Forward.
    /// Веса <c>x</c>/<c>y</c> меняют только доли колонок; условие по составу якорей не зависит от чисел.
    /// </summary>
    public static bool ShouldMaximizeMainWindowAtStartup(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens) =>
        IsPfdForwardCombinedOnFirstScreen(screens)
        || IsTripleOneAnchorPerZonePreset(screens)
        || IsPmPlusForwardTwoScreenPreset(screens);

    /// <summary>
    /// Два дисплея: на одном — только Forward, на другом — только PFD+MFD (без лобового), с весами <c>xP+yM</c>.
    /// Симметрично <c>(F)(xP+yM)</c> и <c>(xP+yM)(F)</c> (ADR 0017).
    /// </summary>
    public static bool IsPmPlusForwardTwoScreenPreset(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens)
    {
        if (screens.Count != 2)
            return false;

        var a = screens[0];
        var b = screens[1];
        return IsPmCombinedScreen(a) && IsForwardOnlyScreen(b)
            || IsForwardOnlyScreen(a) && IsPmCombinedScreen(b);
    }

    /// <summary>
    /// Индекс группы, на которой показывается только Forward в пресете <see cref="IsPmPlusForwardTwoScreenPreset"/>; иначе <c>false</c>.
    /// Главное окно (лобовое) сопоставляется с этим экраном в порядке <see cref="PresentationMonitorTopology.OrderScreensForPresentation"/>.
    /// </summary>
    public static bool TryGetMainWindowPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index)
    {
        index = -1;
        if (IsPmPlusForwardTwoScreenPreset(screens))
        {
            if (IsForwardOnlyScreen(screens[0]))
            {
                index = 0;
                return true;
            }

            if (IsForwardOnlyScreen(screens[1]))
            {
                index = 1;
                return true;
            }

            return false;
        }

        // (P)(F)(M) и перестановки: лобовое — экран с единственным F, не первый экран в строке (ADR 0017).
        return TryGetSingleAnchorScreenIndex(screens, PresentationAnchorKind.Forward, out index);
    }

    /// <summary>Индекс экрана с объединённым <c>P+M</c> для окна-хоста сплита (симметрично <c>(F)</c>).</summary>
    public static bool TryGetPmSplitHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index)
    {
        index = -1;
        if (!IsPmPlusForwardTwoScreenPreset(screens))
            return false;

        if (IsPmCombinedScreen(screens[0]))
        {
            index = 0;
            return true;
        }

        if (IsPmCombinedScreen(screens[1]))
        {
            index = 1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Индекс группы главного окна (лобовое): <see cref="IsPmPlusForwardTwoScreenPreset"/> или тройной <c>(P)(F)(M)</c> — экран с <c>F</c>; иначе <c>0</c>.
    /// </summary>
    public static int GetMainWindowPresentationScreenIndexOrDefault(PresentationParseResult parse)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return 0;
        return TryGetMainWindowPresentationScreenIndex(parse.Screens, out var idx) ? idx : 0;
    }

    private static bool IsForwardOnlyScreen(IReadOnlyList<PresentationAnchorSlot> screen) =>
        screen.Count == 1 && screen[0].Kind == PresentationAnchorKind.Forward;

    /// <summary>На экране есть и PFD, и MFD, и нет лобового (одна группа <c>xP+yM</c>).</summary>
    private static bool IsPmCombinedScreen(IReadOnlyList<PresentationAnchorSlot> screen) =>
        ContainsAnchor(screen, PresentationAnchorKind.Pfd)
        && ContainsAnchor(screen, PresentationAnchorKind.Mfd)
        && !ContainsAnchor(screen, PresentationAnchorKind.Forward);

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
