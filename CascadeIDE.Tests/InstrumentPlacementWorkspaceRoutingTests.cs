using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Tests;

public sealed class InstrumentPlacementWorkspaceRoutingTests
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    [Fact]
    public void Workspace_routing_alias_overrides_default_for_pfd_on_both_surfaces()
    {
        try
        {
            InstrumentPlacementRuntime.ResetToCodeDefaults();
            InstrumentPlacementRuntime.ApplyWorkspaceInstrumentRouting(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map"
                });

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
                    DisplaySettings: new DisplaySettings { PreferRepoInstruments = true },
                    SafetyLevel: AgentSafetyLevel.Confirm));

            Assert.Single(frame.Instruments);
            Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, frame.Instruments[0].InstrumentId);
        }
        finally
        {
            InstrumentPlacementRuntime.ResetToCodeDefaults();
        }
    }
}
