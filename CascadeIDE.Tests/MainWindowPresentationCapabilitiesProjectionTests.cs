using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowPresentationCapabilitiesProjectionTests
{
    private static UiModeCapabilities C(bool ideHealthStrip = true, IdeHealthUiSurface surface = IdeHealthUiSurface.BottomStrip) =>
        new(
            QuickActions: true,
            AgentOperationsPanel: true,
            AgentTrace: false,
            AutonomousAgentTelemetry: false,
            IdeHealthOnTerminalTab: true,
            IdeHealthMainColumnSpan: 5,
            InstrumentationTabs: true,
            HypothesesTab: true,
            RiskSummaryCard: true,
            ResultSummaryCard: true,
            IdeHealthStripVisible: ideHealthStrip,
            IdeHealthSurface: surface,
            ProblemsPanelVisible: true,
            EicasAlertsBarEnabled: true);

    [Fact]
    public void ShowIdeHealthStrip_requires_bottom_strip_and_flag()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowIdeHealthStrip(C()));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowIdeHealthStrip(C(ideHealthStrip: false)));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowIdeHealthStrip(
            C(surface: IdeHealthUiSurface.DedicatedPage)));
    }

    [Fact]
    public void IdeHealthOnTerminalTab_hides_when_bottom_strip_visible()
    {
        var c = C();
        Assert.False(MainWindowPresentationCapabilitiesProjection.IdeHealthOnTerminalTab(c, showIdeHealthStrip: true));
        Assert.True(MainWindowPresentationCapabilitiesProjection.IdeHealthOnTerminalTab(c, showIdeHealthStrip: false));
        var offTab = C() with { IdeHealthOnTerminalTab = false };
        Assert.False(MainWindowPresentationCapabilitiesProjection.IdeHealthOnTerminalTab(offTab, showIdeHealthStrip: false));
    }

    [Fact]
    public void ShowEicasAlertsBar_requires_messages_when_enabled()
    {
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowEicasAlertsBar(C(), 0));
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowEicasAlertsBar(C(), 1));
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowEicasAlertsBar(C() with { EicasAlertsBarEnabled = false }, 3));
    }

    [Fact]
    public void ShowWorkspaceChromeBand_is_or_of_strip_layers()
    {
        Assert.False(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(false, false));
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(true, false));
        Assert.True(MainWindowPresentationCapabilitiesProjection.ShowWorkspaceChromeBand(false, true));
    }

    [Fact]
    public void LocBadgeSummary_empty_when_non_positive_loc()
    {
        Assert.Equal("", MainWindowPresentationCapabilitiesProjection.LocBadgeSummary(0, "High"));
        Assert.Equal("LOC: 3 · Medium", MainWindowPresentationCapabilitiesProjection.LocBadgeSummary(3, "Medium"));
    }

    [Fact]
    public void IsSafetyLevel_is_ordinal_ignore_case()
    {
        Assert.True(MainWindowPresentationCapabilitiesProjection.IsSafetyLevel(AgentSafetyLevel.Observe, AgentSafetyLevel.Observe));
        Assert.False(MainWindowPresentationCapabilitiesProjection.IsSafetyLevel(AgentSafetyLevel.Confirm, AgentSafetyLevel.Observe));
    }
}
