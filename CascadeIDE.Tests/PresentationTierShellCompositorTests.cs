using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Tests;

public sealed class PresentationTierShellCompositorTests
{
    private static PresentationGrammarTokens Grammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    [Fact]
    public void Compact_tier_hides_pfd_and_shows_aux_panel_when_expanded()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", Grammar());
        var display = new DisplaySettings
        {
            Presentation = new DisplayPresentationSettings { CompactAuxiliaryPanelWidthPx = 400 },
        };

        var c = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: true,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 340,
                CollapsedMfdWidthPixels: 8,
                display,
                AgentSafetyLevel.Confirm,
                PresentationTierKind.Compact));

        Assert.False(c.PfdSurfaceVisible);
        Assert.True(c.CompactIdeLayout);
        Assert.True(c.IntercomAuxColumnVisible);
        Assert.False(c.MfdColumnVisibleInMainGrid);
        Assert.Equal(400, c.IntercomAuxColumnPixelWidth);
        Assert.Equal(400, c.CompactRightChromeColumnPixelWidth);
    }

    [Fact]
    public void Compact_tier_bottom_intercom_placement_uses_bottom_dock_not_side_aux()
    {
        var parse = PresentationParser.Parse("(F)", Grammar());
        var display = new DisplaySettings
        {
            Presentation = new DisplayPresentationSettings
            {
                CompactIntercomPlacement = "bottom",
                CompactIntercomBottomDockHeightPx = 300,
            },
        };

        var c = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: false,
                IntentChatPanelExpanded: true,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 340,
                CollapsedMfdWidthPixels: 8,
                display,
                AgentSafetyLevel.Confirm,
                PresentationTierKind.Compact));

        Assert.False(c.IntercomAuxColumnVisible);
        Assert.True(c.IntercomBottomDockVisible);
        Assert.Equal(300, c.IntercomBottomDockHeightPx);
        Assert.False(c.CompactRightChromeColumnVisible);
    }

    [Fact]
    public void Compact_tier_pfd_right_when_solution_explorer_intent()
    {
        var parse = PresentationParser.Parse("(F)", Grammar());
        var display = new DisplaySettings { Presentation = new DisplayPresentationSettings() };

        var c = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: false,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 340,
                CollapsedMfdWidthPixels: 8,
                display,
                AgentSafetyLevel.Confirm,
                PresentationTierKind.Compact));

        Assert.True(c.PfdRightColumnVisible);
        Assert.Equal(220, c.PfdRightColumnPixelWidth);
        Assert.False(c.IntercomAuxColumnVisible);
    }

    [Fact]
    public void Compact_tier_collapsed_aux_panel_zero_width()
    {
        var parse = PresentationParser.Parse("(F)", Grammar());
        var display = new DisplaySettings { Presentation = new DisplayPresentationSettings() };

        var c = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: false,
                IntentChatPanelExpanded: false,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 340,
                CollapsedMfdWidthPixels: 8,
                display,
                AgentSafetyLevel.Confirm,
                PresentationTierKind.Compact));

        Assert.False(c.PfdSurfaceVisible);
        Assert.False(c.MfdColumnVisibleInMainGrid);
        Assert.Equal(0, c.MfdColumnPixelWidthInMainGrid);
    }
}
