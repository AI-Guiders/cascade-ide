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
        || IsPmPlusForwardTwoScreenPreset(screens)
        || IsPmOneOfForwardTwoScreenPreset(screens)
        || IsForwardMfdTwoScreenPreset(screens);

    public static bool ShouldMaximizeMainWindowAtStartup(PresentationParseResult parse) =>
        parse.IsSuccess
        && (IsPfdForwardCombinedOnFirstScreen(parse.Screens)
            || IsTripleOneAnchorPerZonePreset(parse.Screens)
            || IsPmPlusForwardTwoScreenPreset(parse.Screens, parse.ScreenComposes)
            || IsOneOfPlusDedicatedTwoScreenPreset(parse.Screens, parse.ScreenComposes)
            || IsForwardMfdTwoScreenPreset(parse.Screens));

    /// <summary>
    /// Два дисплея: на одном — только Forward, на другом — только MFD.
    /// Симметрично <c>(F)(M)</c> и <c>(M)(F)</c> (ADR 0017, operator 2-monitor default).
    /// </summary>
    public static bool IsForwardMfdTwoScreenPreset(IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens)
    {
        if (screens.Count != 2)
            return false;

        var a = screens[0];
        var b = screens[1];
        return IsForwardOnlyScreen(a) && IsSingleAnchor(b, PresentationAnchorKind.Mfd)
            || IsSingleAnchor(a, PresentationAnchorKind.Mfd) && IsForwardOnlyScreen(b);
    }

    /// <summary>
    /// Два дисплея: на одном — только Forward, на другом — только PFD+MFD (без лобового), сплит <c>xP+yM</c>.
    /// Симметрично <c>(F)(xP+yM)</c> и <c>(xP+yM)(F)</c> (ADR 0017). Not OneOf <c>P/M</c>.
    /// </summary>
    public static bool IsPmPlusForwardTwoScreenPreset(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes = null)
    {
        if (screens.Count != 2)
            return false;

        var a = screens[0];
        var b = screens[1];
        return IsPmSplitCombinedScreen(a, ComposeAt(composes, 0)) && IsForwardOnlyScreen(b)
            || IsForwardOnlyScreen(a) && IsPmSplitCombinedScreen(b, ComposeAt(composes, 1));
    }

    /// <summary>
    /// Два дисплея: Forward + OneOf <c>P/M</c> (full TopLevel XOR). Sym <c>(P/M)(F)</c> / <c>(F)(P/M)</c>.
    /// Subset of <see cref="IsOneOfPlusDedicatedTwoScreenPreset"/> (topology-oneof-slash-v1).
    /// </summary>
    public static bool IsPmOneOfForwardTwoScreenPreset(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes = null) =>
        TryDescribeOneOfPlusDedicatedTwoScreen(screens, composes, out var d, out var a, out var b, out _, out _)
        && d == PresentationAnchorKind.Forward
        && IsUnorderedPair(a, b, PresentationAnchorKind.Pfd, PresentationAnchorKind.Mfd);

    /// <summary>
    /// Два дисплея: один dedicated-якорь + OneOf двух остальных (полный набор P|F|M).
    /// Examples: <c>(F)(P/M)</c> · <c>(P)(F/M)</c> · <c>(M)(P/F)</c> (+ sym). topology-oneof-slash-v1.
    /// </summary>
    public static bool IsOneOfPlusDedicatedTwoScreenPreset(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes = null) =>
        TryDescribeOneOfPlusDedicatedTwoScreen(screens, composes, out _, out _, out _, out _, out _);

    /// <summary>
    /// Describe 2-screen OneOf packing: dedicated singleton + OneOf pair covering {P,F,M}.
    /// </summary>
    public static bool TryDescribeOneOfPlusDedicatedTwoScreen(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes,
        out PresentationAnchorKind dedicated,
        out PresentationAnchorKind oneOfA,
        out PresentationAnchorKind oneOfB,
        out int dedicatedScreen,
        out int oneOfScreen)
    {
        dedicated = default;
        oneOfA = default;
        oneOfB = default;
        dedicatedScreen = -1;
        oneOfScreen = -1;
        if (screens.Count != 2 || composes is null)
            return false;

        for (var i = 0; i < 2; i++)
        {
            var j = 1 - i;
            if (!TryGetOneOfPair(screens[i], ComposeAt(composes, i), out var a, out var b))
                continue;
            if (SingleAnchorKind(screens[j]) is not { } ded)
                continue;
            if (ded == a || ded == b)
                continue;
            if (!HasAllZoneKinds(ded, a, b))
                continue;

            dedicated = ded;
            oneOfA = a;
            oneOfB = b;
            oneOfScreen = i;
            dedicatedScreen = j;
            return true;
        }

        return false;
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
        if (TryGetForwardOnlyMainWindowScreenIndex(screens, out index))
            return true;

        // (P)(F)(M) и перестановки: лобовое — экран с единственным F, не первый экран в строке (ADR 0017).
        return TryGetSingleAnchorScreenIndex(screens, PresentationAnchorKind.Forward, out index);
    }

    /// <summary>Parse-aware: OneOf <c>P/M</c> needs <see cref="PresentationParseResult.ScreenComposes"/>.</summary>
    public static bool TryGetMainWindowPresentationScreenIndex(
        PresentationParseResult parse,
        out int index)
    {
        index = -1;
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return false;

        if (IsPmPlusForwardTwoScreenPreset(parse.Screens, parse.ScreenComposes)
            || IsOneOfPlusDedicatedTwoScreenPreset(parse.Screens, parse.ScreenComposes)
            || IsForwardMfdTwoScreenPreset(parse.Screens))
        {
            // Main = screen that carries Forward (dedicated F, or OneOf that includes F).
            if (TryGetScreenIndexContaining(parse.Screens, PresentationAnchorKind.Forward, out index))
                return true;
        }

        return TryGetSingleAnchorScreenIndex(parse.Screens, PresentationAnchorKind.Forward, out index);
    }

    /// <summary>Индекс экрана с объединённым <c>P+M</c> для окна-хоста сплита (симметрично <c>(F)</c>).</summary>
    public static bool TryGetPmSplitHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index,
        IReadOnlyList<PresentationZoneCompose>? composes = null)
    {
        index = -1;
        if (!IsPmPlusForwardTwoScreenPreset(screens, composes))
            return false;

        if (IsPmSplitCombinedScreen(screens[0], ComposeAt(composes, 0)))
        {
            index = 0;
            return true;
        }

        if (IsPmSplitCombinedScreen(screens[1], ComposeAt(composes, 1)))
        {
            index = 1;
            return true;
        }

        return false;
    }

    /// <summary>Индекс экрана OneOf <c>P/M</c> host (симметрично <c>(F)</c>). Compat alias of OneOf host screen.</summary>
    public static bool TryGetPmOneOfHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose> composes,
        out int index) =>
        TryGetOneOfHostPresentationScreenIndex(screens, composes, out index)
        && IsPmOneOfForwardTwoScreenPreset(screens, composes);

    /// <summary>Индекс экрана generic OneOf host (any pair + dedicated). topology-oneof-slash-v1.</summary>
    public static bool TryGetOneOfHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose> composes,
        out int index)
    {
        index = -1;
        if (!TryDescribeOneOfPlusDedicatedTwoScreen(screens, composes, out _, out _, out _, out _, out var oneOfScreen))
            return false;
        index = oneOfScreen;
        return true;
    }

    /// <summary>
    /// Индекс группы главного окна (лобовое): <see cref="IsPmPlusForwardTwoScreenPreset"/> или тройной <c>(P)(F)(M)</c> — экран с <c>F</c>; иначе <c>0</c>.
    /// </summary>
    public static int GetMainWindowPresentationScreenIndexOrDefault(PresentationParseResult parse)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return 0;
        return TryGetMainWindowPresentationScreenIndex(parse, out var idx) ? idx : 0;
    }

    private static bool IsForwardOnlyScreen(IReadOnlyList<PresentationAnchorSlot> screen) =>
        screen.Count == 1 && screen[0].Kind == PresentationAnchorKind.Forward;

    private static bool TryGetForwardOnlyMainWindowScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index)
    {
        index = -1;
        // Screens-only path cannot see OneOf compose — use TryGetMainWindowPresentationScreenIndex(parse) for P/M.
        if (IsPmPlusForwardTwoScreenPreset(screens) || IsForwardMfdTwoScreenPreset(screens))
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
        }

        return false;
    }

    private static bool TryGetForwardMfdHostPresentationScreenIndex(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        out int index)
    {
        index = -1;
        if (!IsForwardMfdTwoScreenPreset(screens))
            return false;

        if (IsSingleAnchor(screens[0], PresentationAnchorKind.Mfd))
        {
            index = 0;
            return true;
        }

        if (IsSingleAnchor(screens[1], PresentationAnchorKind.Mfd))
        {
            index = 1;
            return true;
        }

        return false;
    }

    /// <summary>На экране есть и PFD, и MFD, и нет лобового — сплит <c>xP+yM</c>.</summary>
    private static bool IsPmSplitCombinedScreen(
        IReadOnlyList<PresentationAnchorSlot> screen,
        PresentationZoneCompose compose) =>
        compose == PresentationZoneCompose.Split
        && ContainsAnchor(screen, PresentationAnchorKind.Pfd)
        && ContainsAnchor(screen, PresentationAnchorKind.Mfd)
        && !ContainsAnchor(screen, PresentationAnchorKind.Forward);

    /// <summary>OneOf group with exactly two distinct anchors (no weights).</summary>
    static bool TryGetOneOfPair(
        IReadOnlyList<PresentationAnchorSlot> screen,
        PresentationZoneCompose compose,
        out PresentationAnchorKind a,
        out PresentationAnchorKind b)
    {
        a = default;
        b = default;
        if (compose != PresentationZoneCompose.OneOf || screen.Count != 2)
            return false;
        if (screen[0].Weight is not null || screen[1].Weight is not null)
            return false;
        a = screen[0].Kind;
        b = screen[1].Kind;
        return a != b;
    }

    static bool IsUnorderedPair(
        PresentationAnchorKind a,
        PresentationAnchorKind b,
        PresentationAnchorKind x,
        PresentationAnchorKind y) =>
        a == x && b == y || a == y && b == x;

    static bool TryGetScreenIndexContaining(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        PresentationAnchorKind kind,
        out int index)
    {
        for (var i = 0; i < screens.Count; i++)
        {
            if (ContainsAnchor(screens[i], kind))
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    static PresentationZoneCompose ComposeAt(IReadOnlyList<PresentationZoneCompose>? composes, int index)
    {
        if (composes is null || (uint)index >= (uint)composes.Count)
            return PresentationZoneCompose.Split;
        return composes[index];
    }

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

        if (TryGetForwardMfdHostPresentationScreenIndex(screens, out index))
            return true;

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
