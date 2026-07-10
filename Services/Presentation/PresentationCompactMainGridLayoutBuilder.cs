namespace CascadeIDE.Services.Presentation;

/// <summary>MainGrid geometry for compact tier (ADR 0171): editor center, optional auxiliary column.</summary>
public static class PresentationCompactMainGridLayoutBuilder
{
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
}
