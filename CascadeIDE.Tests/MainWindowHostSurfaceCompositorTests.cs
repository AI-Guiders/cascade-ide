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
    public void WhenPfdColumnVisible_IncludesPlacedInstrumentsForPfdSlot()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var frame = MainWindowHostSurfaceCompositor.ComposeFrame(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                SafetyLevel: "L2"));

        Assert.True(frame.Shell.PfdSurfaceVisible);
        Assert.Contains(
            frame.Instruments,
            x => x.InstrumentId == CockpitStandardInstrumentIds.SolutionExplorerTree && x.SlotId == CockpitSlotIds.Pfd);
        Assert.Contains(
            frame.Instruments,
            x => x.InstrumentId == CockpitStandardInstrumentIds.WorkspaceHealthStatusV1 && x.SlotId == CockpitSlotIds.Pfd);
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
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                SafetyLevel: "L2"));

        Assert.False(frame.Shell.PfdSurfaceVisible);
        Assert.Empty(frame.Instruments);
    }

    [Fact]
    public void WhenMfdColumnVisible_IncludesWorkspaceHealthInstrumentInMfdSlot()
    {
        var parse = PresentationParser.Parse("(P+F+M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var frame = MainWindowHostSurfaceCompositor.ComposeFrame(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                SafetyLevel: "L2"));

        Assert.True(frame.Shell.MfdColumnVisibleInMainGrid);
        Assert.Contains(
            frame.Instruments,
            x => x.InstrumentId == CockpitStandardInstrumentIds.WorkspaceHealthStatusV1 && x.SlotId == CockpitSlotIds.Mfd);
    }
}
