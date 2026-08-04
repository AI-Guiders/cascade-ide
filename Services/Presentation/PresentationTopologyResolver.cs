namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Снимок флагов пресета <c>presentation</c> — единый источник для VM и тестов (ADR 0017 · topology-oneof-slash-v0).
/// </summary>
public readonly record struct PresentationTopologyFlags(
    bool DedicatedMfdSecondScreen,
    bool TripleOneAnchorPerZone,
    bool ForwardMfdTwoScreen,
    bool PmForwardTwoScreen,
    bool PmOneOfForwardTwoScreen,
    bool OneOfPlusDedicatedTwoScreen)
{
    /// <summary>Нужен <see cref="Views.MfdHostWindow"/> (второй TopLevel с MFD).</summary>
    public bool MfdHostTopology =>
        DedicatedMfdSecondScreen || TripleOneAnchorPerZone || ForwardMfdTwoScreen;

    /// <summary>Нужен <see cref="Views.PfdHostWindow"/> (тройной пресет по одному якорю на экран).</summary>
    public bool PfdHostTopology => TripleOneAnchorPerZone;

    /// <summary>Нужен PM split host (<c>P+M</c> columns).</summary>
    public bool PmHostTopology => PmForwardTwoScreen;

    /// <summary>Нужен PM OneOf host (<c>P/M</c> XOR full zone) — v0 compat subset.</summary>
    public bool PmOneOfHostTopology => PmOneOfForwardTwoScreen;

    /// <summary>Нужен generic OneOf host (any pair + dedicated) — topology-oneof-slash-v1.</summary>
    public bool OneOfHostTopology => OneOfPlusDedicatedTwoScreen;
}

/// <summary>Разбор строки <c>presentation</c> в флаги хостов и кадр <c>MainGrid</c>.</summary>
public static class PresentationTopologyResolver
{
    public static PresentationTopologyFlags ResolveFlags(PresentationParseResult parse)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return default;

        return ResolveFlags(parse.Screens, parse.ScreenComposes);
    }

    public static PresentationTopologyFlags ResolveFlags(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens) =>
        ResolveFlags(screens, composes: null);

    public static PresentationTopologyFlags ResolveFlags(
        IReadOnlyList<IReadOnlyList<PresentationAnchorSlot>> screens,
        IReadOnlyList<PresentationZoneCompose>? composes) =>
        new(
            PresentationLayoutAnalyzer.IsDedicatedMfdSecondScreenPreset(screens),
            PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(screens),
            PresentationLayoutAnalyzer.IsForwardMfdTwoScreenPreset(screens),
            PresentationLayoutAnalyzer.IsPmPlusForwardTwoScreenPreset(screens, composes),
            PresentationLayoutAnalyzer.IsPmOneOfForwardTwoScreenPreset(screens, composes),
            PresentationLayoutAnalyzer.IsOneOfPlusDedicatedTwoScreenPreset(screens, composes));

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
