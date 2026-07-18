namespace CascadeIDE.Services.Presentation;

/// <summary>MainGrid geometry for compact tier (ADR 0171): editor center, optional auxiliary column, optional bottom MFD dock.</summary>
public static class PresentationCompactMainGridLayoutBuilder
{
    public const string CockpitRowDefinitions = "Auto,Auto,*";

    public static string BuildRowDefinitions(bool intercomBottomVisible, bool mfdBottomVisible)
    {
        if (!intercomBottomVisible && !mfdBottomVisible)
            return CockpitRowDefinitions;

        if (intercomBottomVisible && mfdBottomVisible)
            return "Auto,Auto,*,4,Auto,4,Auto";

        return "Auto,Auto,*,4,Auto";
    }

    public static PresentationMainGridLayoutFrame Build(
        bool auxiliaryPanelExpanded,
        bool suppressAuxiliaryForHost,
        int auxiliaryPanelWidthPx,
        int collapsedAuxiliaryWidthPx)
    {
        if (suppressAuxiliaryForHost || !auxiliaryPanelExpanded)
        {
            return new PresentationMainGridLayoutFrame(
                "0,4,*,4,0",
                1,
                false,
                new[] { 1.0 },
                new[] { new PresentationZoneBound(PresentationAnchorKind.Forward, 0.0, 1.0) });
        }

        var aux = Math.Max(collapsedAuxiliaryWidthPx, auxiliaryPanelWidthPx);
        var columns = $"0,4,*,4,{aux}";
        return new PresentationMainGridLayoutFrame(
            columns,
            2,
            false,
            new[] { 0.75, 0.25 },
            new[]
            {
                new PresentationZoneBound(PresentationAnchorKind.Forward, 0.0, 0.75),
                new PresentationZoneBound(PresentationAnchorKind.Mfd, 0.75, 0.25),
            });
    }

    /// <summary>Compact right chrome column width (Intercom aux and/or PFD right).</summary>
    public static PresentationMainGridLayoutFrame BuildWithRightChromeWidth(
        int rightChromeWidthPx,
        int collapsedAuxiliaryWidthPx)
    {
        if (rightChromeWidthPx <= 0)
        {
            return new PresentationMainGridLayoutFrame(
                "0,4,*,4,0",
                1,
                false,
                new[] { 1.0 },
                new[] { new PresentationZoneBound(PresentationAnchorKind.Forward, 0.0, 1.0) });
        }

        var aux = Math.Max(collapsedAuxiliaryWidthPx, rightChromeWidthPx);
        var columns = $"0,4,*,4,{aux}";
        return new PresentationMainGridLayoutFrame(
            columns,
            2,
            false,
            new[] { 0.75, 0.25 },
            new[]
            {
                new PresentationZoneBound(PresentationAnchorKind.Forward, 0.0, 0.75),
                new PresentationZoneBound(PresentationAnchorKind.Mfd, 0.75, 0.25),
            });
    }
}
