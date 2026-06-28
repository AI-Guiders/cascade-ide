namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Снимок флагов пресета <c>presentation</c> — единый источник для VM и тестов (ADR 0017).
/// </summary>
public readonly record struct PresentationTopologyFlags(
    bool DedicatedMfdSecondScreen,
    bool TripleOneAnchorPerZone,
    bool ForwardMfdTwoScreen,
    bool PmForwardTwoScreen)
{
    /// <summary>Нужен <see cref="Views.MfdHostWindow"/> (второй TopLevel с MFD).</summary>
    public bool MfdHostTopology =>
        DedicatedMfdSecondScreen || TripleOneAnchorPerZone || ForwardMfdTwoScreen;

    /// <summary>Нужен <see cref="Views.PfdHostWindow"/> (тройной пресет по одному якорю на экран).</summary>
    public bool PfdHostTopology => TripleOneAnchorPerZone;

    /// <summary>Нужен <see cref="Views.PmSplitHostWindow"/>.</summary>
    public bool PmHostTopology => PmForwardTwoScreen;
}

/// <summary>Разбор строки <c>presentation</c> в флаги хостов и кадр <c>MainGrid</c>.</summary>
public static class PresentationTopologyResolver
{
    public static PresentationTopologyFlags ResolveFlags(PresentationParseResult parse)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return default;

        return ResolveFlags(parse.Screens);
    }

    public static PresentationTopologyFlags ResolveFlags(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens) =>
        new(
            PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(screens),
            PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(screens),
            PresentationLayoutAnalyzer.IsForwardMfdTwoScreenPreset(screens),
            PresentationLayoutAnalyzer.IsPmPlusForwardTwoScreenPreset(screens));

    /// <summary>
    /// Кадр колонок главного окна при типичном старте: хосты PFD/MFD открыты и подавляют свои колонки в main.
    /// </summary>
    public static PresentationMainGridLayoutFrame BuildMainWindowGridAtStartup(
        PresentationParseResult parse,
        PresentationTopologyFlags flags)
    {
        var mainIdx = PresentationLayoutAnalyzer.GetMainWindowPresentationScreenIndexOrDefault(parse);
        return PresentationMainGridLayoutFrameBuilder.Build(
            parse,
            flags.DedicatedMfdSecondScreen,
            mfdColumnSuppressedForHost: flags.MfdHostTopology,
            flags.TripleOneAnchorPerZone,
            suppressPfdColumnForPfdHostWindow: flags.PfdHostTopology,
            mainIdx);
    }
}
