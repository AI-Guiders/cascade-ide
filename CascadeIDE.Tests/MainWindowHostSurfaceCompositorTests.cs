using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Services.Presentation;
using Xunit;

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
                IntentPfdRegionExpanded: true,
                IntentMfdRegionExpanded: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                SafetyLevel: "L2"));

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
                IntentPfdRegionExpanded: false,
                IntentMfdRegionExpanded: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                SafetyLevel: "L2"));

        Assert.False(frame.Shell.PfdSurfaceVisible);
        Assert.Empty(frame.Instruments);
    }
}
