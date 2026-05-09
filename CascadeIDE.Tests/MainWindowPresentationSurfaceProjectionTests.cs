using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowPresentationSurfaceProjectionTests
{
    [Fact]
    public void ResolveWindowTitle_matches_catalog_then_family_fallback()
    {
        foreach (var mode in new[] { "Flight", "Editor", "Debug", "" })
        {
            var normalized = UiModeCatalog.NormalizeUiMode(string.IsNullOrEmpty(mode) ? null : mode);
            var fromCatalog = UiModeCatalog.GetWindowTitleOverride(normalized);
            string expected = fromCatalog
                ?? UiModeFamilyResolver.FromNormalizedMode(normalized) switch
                {
                    UiModeFamily.Power => "CascadeIDE — Power Mode [Autonomous Agent Cockpit]",
                    UiModeFamily.AgentChat => "CascadeIDE — Agent Chat",
                    UiModeFamily.Debug => "CascadeIDE — Debug",
                    UiModeFamily.Editor => "CascadeIDE — Editor",
                    _ => "CascadeIDE",
                };
            Assert.Equal(expected, MainWindowPresentationSurfaceProjection.ResolveWindowTitle(normalized));
        }
    }

    [Fact]
    public void IsMfdContourContentVisible_requires_any_checked_layer()
    {
        Assert.False(MainWindowPresentationSurfaceProjection.IsMfdContourContentVisible(
            problemsPanelVisible: false,
            isTerminalVisible: false,
            isBuildOutputVisible: false,
            instrumentationTabs: false,
            isGitPanelVisible: false));
        Assert.True(MainWindowPresentationSurfaceProjection.IsMfdContourContentVisible(
            problemsPanelVisible: true,
            isTerminalVisible: false,
            isBuildOutputVisible: false,
            instrumentationTabs: false,
            isGitPanelVisible: false));
    }

    [Fact]
    public void MountPolicySurfaceId_docked_grid_stable()
    {
        Assert.Equal(
            "main_window_docked_grid",
            MainWindowPresentationSurfaceProjection.MountPolicySurfaceId(AttentionLayoutSurfaceKind.MainWindowDockedGrid));
    }

    [Fact]
    public void IsAgentSummary_visible_only_when_differs_from_placeholder()
    {
        var p = MainWindowPresentationSurfaceProjection.DefaultRiskSummaryPlaceholder;
        Assert.False(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder(null, p));
        Assert.False(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder("  ", p));
        Assert.False(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder(p, p));
        Assert.True(MainWindowPresentationSurfaceProjection.IsAgentSummaryVisibleComparedToPlaceholder("risk text", p));
    }
}
