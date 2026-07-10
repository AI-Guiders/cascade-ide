using System.Globalization;

namespace CascadeIDE.Services.Presentation;

/// <summary>Triple-zone cockpit on one ultrawide canvas (ADR 0171 P3).</summary>
public static class PresentationUltrawideCockpitLayoutBuilder
{
    public static PresentationMainGridLayoutFrame Build(
        int totalWidthPx,
        int minAnchorWidthPx,
        bool suppressPfdHost,
        bool suppressMfdHost)
    {
        var anchor = Math.Max(320, minAnchorWidthPx);
        var pfd = suppressPfdHost ? 0 : Math.Min(anchor, totalWidthPx / 5);
        var mfd = suppressMfdHost ? 0 : Math.Min(anchor, totalWidthPx / 5);
        var columns = $"{pfd},4,*,4,{mfd}";
        return new PresentationMainGridLayoutFrame(
            columns,
            3,
            false,
            Array.Empty<double>(),
            Array.Empty<PresentationZoneBound>());
    }

    public static string FormatWeightedTriple(double wP, double wF, double wM) =>
        $"{FormatWeight(wP)}*,4,{FormatWeight(wF)}*,4,{FormatWeight(wM)}*";

    private static string FormatWeight(double w) => w.ToString("0.########", CultureInfo.InvariantCulture);
}
