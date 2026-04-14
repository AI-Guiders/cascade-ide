using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowShellSurfaceCompositorTests
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    [Fact]
    public void WhenHostSuppressesMfd_ColumnWidthZeroEvenIfIntentExpanded()
    {
        var parse = PresentationParser.Parse("(P+F) (M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var c = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: true,
                SuppressMfdColumnForMfdHostWindow: true,
                ExpandedMfdWidthPixels: 340,
                CollapsedMfdWidthPixels: 8));

        Assert.True(c.PfdSurfaceVisible);
        Assert.True(c.MfdSurfaceExpanded);
        Assert.False(c.MfdColumnVisibleInMainGrid);
        Assert.Equal(0, c.MfdColumnPixelWidthInMainGrid);
    }

    [Fact]
    public void WhenMRequiredOnFirstScreen_IntentCollapsedStillExpandedSurface()
    {
        var parse = PresentationParser.Parse("(P+F+M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);

        var c = MainWindowShellSurfaceCompositor.Compose(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: true,
                IntentChatPanelExpanded: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12));

        Assert.True(c.MfdSurfaceExpanded);
        Assert.True(c.MfdColumnVisibleInMainGrid);
        Assert.Equal(300, c.MfdColumnPixelWidthInMainGrid);
    }
}
