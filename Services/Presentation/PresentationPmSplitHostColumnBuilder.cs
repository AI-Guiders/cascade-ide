using System.Globalization;

namespace CascadeIDE.Services.Presentation;

/// <summary>
/// Колонки сетки окна-хоста сплита P+M (одна группа <c>(xP+yM)</c>), без лобового; порядок колонок — как в строке презентации.
/// </summary>
public static class PresentationPmSplitHostColumnBuilder
{
    private const string DefaultThreeColumn = "220,4,340";

    public static string Build(PresentationParseResult parse, int pmScreenIndex)
    {
        if (!parse.IsSuccess || parse.Screens.Count == 0)
            return DefaultThreeColumn;

        var idx = Math.Clamp(pmScreenIndex, 0, parse.Screens.Count - 1);
        var screen = parse.Screens[idx];
        if (screen.Count != 2)
            return DefaultThreeColumn;

        if (!PresentationZoneWeights.TryNormalize(screen, out var normalized) || normalized.Count != 2)
            return DefaultThreeColumn;

        var w0 = normalized[0];
        var w1 = normalized[1];
        return $"{FormatWeight(w0)}*,4,{FormatWeight(w1)}*";
    }

    private static string FormatWeight(double w) => w.ToString("0.########", CultureInfo.InvariantCulture);
}
