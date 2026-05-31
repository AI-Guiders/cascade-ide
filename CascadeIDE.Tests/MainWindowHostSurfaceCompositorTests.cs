using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Tests;

public sealed class MainWindowHostSurfaceCompositorTests
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    [Fact]
    public void WhenPfdColumnVisible_IncludesSolutionExplorerInstrumentInPfdSlot()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var frame = MainWindowHostSurfaceCompositor.ComposeFrame(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: false,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                DisplaySettings: new DisplaySettings(),
                SafetyLevel: AgentSafetyLevel.Confirm));

        Assert.True(frame.Shell.PfdSurfaceVisible);
        Assert.Single(frame.Instruments);
        Assert.Equal(CockpitStandardInstrumentIds.SolutionExplorerTree, frame.Instruments[0].InstrumentId);
        Assert.Equal(CockpitSlotIds.Pfd, frame.Instruments[0].SlotId);
    }

    [Fact]
    public void WhenPfdColumnHidden_NoInstruments()
    {
        var parse = PresentationParser.Parse("(F) (M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var frame = MainWindowHostSurfaceCompositor.ComposeFrame(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: false,
                IntentChatPanelExpanded: false,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                DisplaySettings: new DisplaySettings(),
                SafetyLevel: AgentSafetyLevel.Confirm));

        Assert.False(frame.Shell.PfdSurfaceVisible);
        Assert.Empty(frame.Instruments);
    }

    [Fact]
    public void UserPlacementRule_OverridesDefaultInstrumentForPfdSlot()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var display = new DisplaySettings
        {
            Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map"
            }
        };

        var frame = MainWindowHostSurfaceCompositor.ComposeFrame(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: false,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                DisplaySettings: display,
                SafetyLevel: AgentSafetyLevel.Confirm));

        Assert.True(frame.Shell.PfdSurfaceVisible);
        Assert.Single(frame.Instruments);
        Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, frame.Instruments[0].InstrumentId);
        Assert.Equal(CockpitSlotIds.Pfd, frame.Instruments[0].SlotId);
    }
}
