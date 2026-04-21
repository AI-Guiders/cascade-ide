using System.Globalization;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Сборщик кадра геометрии <c>MainGrid</c> из строки <c>presentation</c>.
/// Инкапсулирует правила v1 для долей и fallback-веток.
/// </summary>
public static class PresentationMainGridLayoutFrameBuilder
{
    /// <summary>Дефолт без весов/валидной конфигурации в первом экране (5 колонок MainGrid: PFD, splitter, Forward, splitter, MFD).</summary>
    public const string DefaultColumnDefinitions = "220,4,*,4,340";

    /// <param name="mainWindowPresentationScreenIndex">
    /// Индекс группы <c>(…)</c> в строке <c>presentation</c>, которой соответствует главное окно (лобовое).
    /// Для <c>(xP+yM)(F)</c> — группа с <c>F</c> (не всегда экран 0). Иначе используйте <c>0</c>.
    /// </param>
    public static PresentationMainGridLayoutFrame Build(
        PresentationParseResult parse,
        bool dedicatedMfdSecondScreen,
        bool mfdColumnSuppressedForHost,
        bool tripleOneAnchorPerZone,
        bool suppressPfdColumnForPfdHostWindow,
        int mainWindowPresentationScreenIndex = 0)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return DefaultFrame(0);

        if (tripleOneAnchorPerZone
            && PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(parse.Screens))
        {
            return BuildTripleMainWindowFrame(mfdColumnSuppressedForHost, suppressPfdColumnForPfdHostWindow);
        }

        var clampedMain = Math.Clamp(mainWindowPresentationScreenIndex, 0, parse.Screens.Count - 1);
        var mainScreen = parse.Screens[clampedMain];

        if (PresentationLayoutAnalyzer.IsPmPlusForwardTwoScreenPreset(parse.Screens)
            && IsForwardOnlyMainScreen(mainScreen))
        {
            return BuildForwardOnlyMainWindowFrame();
        }

        if (mainScreen.Count is < 2 or > 3)
            return DefaultFrame(mainScreen.Count);

        if (!PresentationZoneWeights.TryNormalize(mainScreen, out var normalized))
            return DefaultFrame(mainScreen.Count);

        var hasExplicitWeights = HasExplicitWeights(mainScreen);
        if (!hasExplicitWeights)
        {
            // Для пресетов без коэффициентов сохраняем исторический layout.
            return new PresentationMainGridLayoutFrame(
                DefaultColumnDefinitions,
                mainScreen.Count,
                hasExplicitWeights,
                normalized,
                BuildZoneBounds(mainScreen, normalized));
        }

        var columns = mainScreen.Count switch
        {
            3 => FormatTriple(normalized[0], normalized[1], normalized[2]),
            2 => FormatDual(normalized[0], normalized[1], dedicatedMfdSecondScreen, mfdColumnSuppressedForHost),
            _ => DefaultColumnDefinitions
        };

        return new PresentationMainGridLayoutFrame(
            columns,
            mainScreen.Count,
            hasExplicitWeights,
            normalized,
            BuildZoneBounds(mainScreen, normalized));
    }

    private static bool IsForwardOnlyMainScreen(IReadOnlyList<PresentationAnchorSlot> mainScreen) =>
        mainScreen.Count == 1 && mainScreen[0].Kind == PresentationAnchorKind.Forward;

    /// <summary>Только лобовое: колонки PFD/MFD нулевые, центральная <c>*</c>.</summary>
    private static PresentationMainGridLayoutFrame BuildForwardOnlyMainWindowFrame() =>
        new(
            "0,4,*,4,0",
            1,
            false,
            new[] { 1.0 },
            new[] { new PresentationZoneBound(PresentationAnchorKind.Forward, 0.0, 1.0) });

    private static PresentationMainGridLayoutFrame DefaultFrame(int contentZoneCount) =>
        new(DefaultColumnDefinitions, contentZoneCount, false, Array.Empty<double>(), Array.Empty<PresentationZoneBound>());

    /// <summary>
    /// Три физических экрана под <c>P</c>/<c>F</c>/<c>M</c>: в главном окне остаётся лобовое; колонки P/M — по флагам подавления хостов.
    /// </summary>
    private static PresentationMainGridLayoutFrame BuildTripleMainWindowFrame(
        bool mfdColumnSuppressedForHost,
        bool suppressPfdColumnForPfdHostWindow)
    {
        const string defaultPfd = "220";
        const string defaultMfdTail = "340";
        var pfdCol = suppressPfdColumnForPfdHostWindow ? "0" : defaultPfd;
        var mfdCol = mfdColumnSuppressedForHost ? "0" : defaultMfdTail;
        var columns = $"{pfdCol},4,*,4,{mfdCol}";
        return new PresentationMainGridLayoutFrame(
            columns,
            3,
            false,
            Array.Empty<double>(),
            Array.Empty<PresentationZoneBound>());
    }

    private static bool HasExplicitWeights(IReadOnlyList<PresentationAnchorSlot> first)
    {
        for (var i = 0; i < first.Count; i++)
        {
            if (first[i].Weight.HasValue)
                return true;
        }

        return false;
    }

    private static string FormatTriple(double wP, double wF, double wM) =>
        $"{FormatWeight(wP)}*,4,{FormatWeight(wF)}*,4,{FormatWeight(wM)}*";

    private static string FormatDual(double w0, double w1, bool dedicatedMfdSecondScreen, bool mfdColumnSuppressedForHost)
    {
        var tail = dedicatedMfdSecondScreen && mfdColumnSuppressedForHost ? "0" : "340";
        return $"{FormatWeight(w0)}*,4,{FormatWeight(w1)}*,4,{tail}";
    }

    private static string FormatWeight(double w) => w.ToString("0.########", CultureInfo.InvariantCulture);

    private static IReadOnlyList<PresentationZoneBound> BuildZoneBounds(
        IReadOnlyList<PresentationAnchorSlot> first,
        IReadOnlyList<double> normalized)
    {
        if (first.Count == 0 || first.Count != normalized.Count)
            return Array.Empty<PresentationZoneBound>();

        var bounds = new PresentationZoneBound[first.Count];
        var cursor = 0.0;
        for (var i = 0; i < first.Count; i++)
        {
            var width = normalized[i];
            bounds[i] = new PresentationZoneBound(first[i].Kind, cursor, width);
            cursor += width;
        }

        return bounds;
    }
}

