using CascadeIDE.Contracts;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.Features.Shell.Application;

/// <summary>
/// Видимость и составные флаги из <see cref="UiModeCapabilities"/> (UI cluster Presentation — без формул режима на главном VM).
/// </summary>
[PresentationProjection]
public static class MainWindowPresentationCapabilitiesProjection
{
    public static bool ShowIdeHealthStrip(UiModeCapabilities c) =>
        c.IdeHealthStripVisible && c.IdeHealthSurface == IdeHealthUiSurface.BottomStrip;

    public static bool IdeHealthOnTerminalTab(UiModeCapabilities c, bool showIdeHealthStrip) =>
        c.IdeHealthOnTerminalTab && !showIdeHealthStrip;

    public static bool ShowIdeHealthMfdPage(UiModeCapabilities c) =>
        c.IdeHealthStripVisible && c.IdeHealthSurface == IdeHealthUiSurface.DedicatedPage;

    public static bool ShowEicasAlertsBar(UiModeCapabilities c, int eicasMessageCount) =>
        c.EicasAlertsBarEnabled && eicasMessageCount > 0;

    public static bool ShowWorkspaceChromeBand(
        bool showIdeHealthStrip,
        bool showEicasAlertsBar,
        bool showAgentCabinChromeHint = false,
        bool showAgentPressureChromeHint = false,
        bool showAgentIgniteChromeHint = false,
        bool showAgentScopeChromeHint = false,
        bool showAgentSysChromeHint = false,
        bool showAgentOnboardChromeHint = false,
        bool showAgentArchChromeHint = false,
        bool showAgentMcpChromeHint = false,
        bool showAgentPlanChromeHint = false,
        bool showAgentReportChromeHint = false,
        bool showAgentCrmChromeHint = false,
        bool showAgentWebcamChromeHint = false,
        bool showAgentToolchainChromeHint = false,
        bool showAgentPluginsChromeHint = false,
        bool showAgentRefactorChromeHint = false,
        bool showAgentReviewChromeHint = false,
        bool showAgentLearnChromeHint = false,
        bool showAgentDomainChromeHint = false,
        bool showAgentSaDeskChromeHint = false) =>
        showIdeHealthStrip
        || showEicasAlertsBar
        || showAgentCabinChromeHint
        || showAgentPressureChromeHint
        || showAgentIgniteChromeHint
        || showAgentScopeChromeHint
        || showAgentSysChromeHint
        || showAgentOnboardChromeHint
        || showAgentArchChromeHint
        || showAgentMcpChromeHint
        || showAgentPlanChromeHint
        || showAgentReportChromeHint
        || showAgentCrmChromeHint
        || showAgentWebcamChromeHint
        || showAgentToolchainChromeHint
        || showAgentPluginsChromeHint
        || showAgentRefactorChromeHint
        || showAgentReviewChromeHint
        || showAgentLearnChromeHint
        || showAgentDomainChromeHint
        || showAgentSaDeskChromeHint;

    public static bool ShowWorkspaceBottomChrome(
        bool showIdeHealthStrip,
        bool showEicasAlertsBar,
        bool isMfdContourContentVisible) =>
        showIdeHealthStrip || showEicasAlertsBar || isMfdContourContentVisible;

    public static bool InstrumentationTabs(bool isInstrumentationDockVisible, UiModeCapabilities c) =>
        isInstrumentationDockVisible && c.InstrumentationTabs;

    public static bool HypothesesTab(bool isInstrumentationDockVisible, UiModeCapabilities c) =>
        isInstrumentationDockVisible && c.InstrumentationTabs && c.HypothesesTab;

    public static bool IsRiskCardVisible(UiModeCapabilities c, bool isRiskSummaryVisible) =>
        c.RiskSummaryCard && isRiskSummaryVisible;

    public static bool IsResultCardVisible(UiModeCapabilities c, bool isResultSummaryVisible) =>
        c.ResultSummaryCard && isResultSummaryVisible;

    public static bool IsSkiaZoneGeometryOverlayPfdVisible(bool zoneGeometryOverlay, bool isPfdColumnVisible) =>
        zoneGeometryOverlay && isPfdColumnVisible;

    public static bool IsSkiaZoneGeometryOverlayForwardVisible(bool zoneGeometryOverlay) =>
        zoneGeometryOverlay;

    public static bool IsSkiaZoneGeometryOverlayMfdVisible(bool zoneGeometryOverlay, bool isMfdColumnVisible) =>
        zoneGeometryOverlay && isMfdColumnVisible;

    public static bool IsSafetyLevel(string? safetyLevel, string expected) =>
        string.Equals(safetyLevel, expected, StringComparison.OrdinalIgnoreCase);

    public static string LocBadgeSummary(int locBadge, string locTierLabel) =>
        locBadge <= 0 ? "" : $"LOC: {locBadge} · {locTierLabel}";
}
