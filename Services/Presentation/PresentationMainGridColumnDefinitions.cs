using System.Globalization;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Строка колонок Avalonia <c>Grid</c> для строки рабочей области главного окна:
/// пять колонок — контент PFD, сплиттер 4px, Forward, сплиттер 4px, MFD (см. <c>MainWindow.axaml</c>).
/// При заданных весах в <c>presentation</c> контент-колонки получают звёздочные доли (<c>*</c>).
/// </summary>
public static class PresentationMainGridColumnDefinitions
{
    /// <summary>Дефолт без весов в строке <c>presentation</c> (как в разметке до весов).</summary>
    public const string Default = "220,4,*,4,340";

    private const double SplitterWidthPx = 4;

    /// <param name="mfdColumnSuppressedForHost">Колонка MFD в главном окне свёрнута — узкий хвост для двухякорного пресета с весами.</param>
    public static string Get(
        PresentationParseResult parse,
        bool dedicatedMfdSecondScreen,
        bool mfdColumnSuppressedForHost)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return Default;

        var first = parse.Screens[0];
        if (first.Count is < 2 or > 3)
            return Default;

        if (!AllSameWeightMode(first, out var allWeighted))
            return Default;

        if (!allWeighted)
            return Default;

        return first.Count switch
        {
            3 => FormatTriple(first[0].Weight!.Value, first[1].Weight!.Value, first[2].Weight!.Value),
            2 => FormatDual(
                first[0].Weight!.Value,
                first[1].Weight!.Value,
                dedicatedMfdSecondScreen,
                mfdColumnSuppressedForHost),
            _ => Default,
        };
    }

    private static bool AllSameWeightMode(IReadOnlyList<PresentationAnchorSlot> first, out bool allWeighted)
    {
        allWeighted = false;
        if (first.Count == 0)
            return true;

        var w0 = first[0].Weight;
        allWeighted = w0.HasValue;
        for (var i = 1; i < first.Count; i++)
        {
            if (first[i].Weight.HasValue != allWeighted)
                return false;
        }

        return true;
    }

    private static string FormatTriple(double wP, double wF, double wM) =>
        $"{FormatWeight(wP)}*,{SplitterWidthPx},{FormatWeight(wF)}*,{SplitterWidthPx},{FormatWeight(wM)}*";

    private static string FormatDual(double w0, double w1, bool dedicatedMfdSecondScreen, bool mfdColumnSuppressedForHost)
    {
        var tail = dedicatedMfdSecondScreen && mfdColumnSuppressedForHost ? "0" : "340";
        return $"{FormatWeight(w0)}*,{SplitterWidthPx},{FormatWeight(w1)}*,{SplitterWidthPx},{tail}";
    }

    private static string FormatWeight(double w) => w.ToString("0.########", CultureInfo.InvariantCulture);
}
