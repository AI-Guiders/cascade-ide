using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowHostSurfaceProjectionTests
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    private sealed class FakeHostSurfaceInput : IMainWindowHostSurfaceInput
    {
        public required PresentationParseResult PresentationParse { get; init; }
        public required bool IsPfdRegionExpanded { get; init; }
        public required bool IsMfdRegionExpanded { get; init; }
        public required bool IsPfdHostWindowShellOpen { get; init; }
        public required bool IsMfdHostWindowShellOpen { get; init; }
        public required DisplaySettings DisplaySettings { get; init; }
        public required string SafetyLevel { get; init; }
    }

    [Fact]
    public void BuildShellInput_maps_host_members_to_shell_composition_record()
    {
        var parse = PresentationParser.Parse("(P+F+M)", DefaultGrammar());
        Assert.True(parse.IsSuccess);
        var display = new DisplaySettings();
        IMainWindowHostSurfaceInput host = new FakeHostSurfaceInput
        {
            PresentationParse = parse,
            IsPfdRegionExpanded = true,
            IsMfdRegionExpanded = false,
            IsPfdHostWindowShellOpen = true,
            IsMfdHostWindowShellOpen = false,
            DisplaySettings = display,
            SafetyLevel = "L1",
        };

        var input = MainWindowHostSurfaceProjection.BuildShellInput(host, expandedMfdWidthPixels: 400, collapsedMfdWidthPixels: 8);

        Assert.Equal(parse, input.PresentationParse);
        Assert.True(input.IntentSolutionExplorerVisible);
        Assert.False(input.IntentChatPanelExpanded);
        Assert.True(input.SuppressPfdColumnForPfdHostWindow);
        Assert.False(input.SuppressMfdColumnForMfdHostWindow);
        Assert.Equal(400, input.ExpandedMfdWidthPixels);
        Assert.Equal(8, input.CollapsedMfdWidthPixels);
        Assert.Same(display, input.DisplaySettings);
        Assert.Equal("L1", input.SafetyLevel);
    }
}
