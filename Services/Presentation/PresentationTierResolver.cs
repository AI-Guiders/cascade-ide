using CascadeIDE.Models;

namespace CascadeIDE.Services.Presentation;

/// <summary>Resolves effective presentation tier from settings, topology, and monitors (ADR 0171).</summary>
public static class PresentationTierResolver
{
    public static PresentationTierKind Resolve(
        DisplayPresentationSettings settings,
        PresentationParseResult parse,
        PresentationMonitorSnapshot monitors)
    {
        var configured = settings.Tier?.Trim() ?? PresentationTierKindExtensions.AutoValue;
        if (string.Equals(configured, PresentationTierKindExtensions.CompactValue, StringComparison.OrdinalIgnoreCase))
            return PresentationTierKind.Compact;
        if (string.Equals(configured, PresentationTierKindExtensions.CockpitValue, StringComparison.OrdinalIgnoreCase))
            return PresentationTierKind.Cockpit;

        return ResolveAuto(settings, parse, monitors);
    }

    public static PresentationTierKind ResolveAuto(
        DisplayPresentationSettings settings,
        PresentationParseResult parse,
        PresentationMonitorSnapshot monitors)
    {
        if (parse.IsSuccess
            && PresentationLayoutAnalyzer.IsTripleOneAnchorPerZonePreset(parse.Screens)
            && monitors.PhysicalScreenCount >= 3)
            return PresentationTierKind.Cockpit;

        if (monitors.PhysicalScreenCount >= 3)
            return PresentationTierKind.Cockpit;

        if (monitors.PhysicalScreenCount == 1
            && settings.UltrawideCockpitEnabled
            && IsUltrawideCockpitCapable(settings, monitors))
            return PresentationTierKind.Cockpit;

        return PresentationTierKind.Compact;
    }

    public static bool IsUltrawideCockpitCapable(
        DisplayPresentationSettings settings,
        PresentationMonitorSnapshot monitors) =>
        monitors.PrimaryWorkingAreaWidthPx >= settings.CockpitMinTotalWidthPx
        && monitors.PrimaryWorkingAreaWidthPx >= settings.CockpitMinAnchorWidthPx * 3;

    /// <summary>Recommendation text for first-run wizard.</summary>
    public static PresentationTierKind RecommendForFirstRun(
        DisplayPresentationSettings settings,
        PresentationParseResult parse,
        PresentationMonitorSnapshot monitors) =>
        ResolveAuto(settings, parse, monitors);
}
