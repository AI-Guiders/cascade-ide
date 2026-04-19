using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Регрессии вида «открыли решение — дерево не там»: одна карта инструментов (<see cref="InstrumentPlacementRuntime"/> + <see cref="DisplaySettings"/>)
/// должна давать тот же смысл, что и кадр композитора и что и <see cref="MainWindowDockedGridInstrumentSlots"/> (те же биндинги, что у <c>MainWindowViewModel</c>).
/// </summary>
[Collection("UiModeCatalog")]
public sealed class SolutionExplorerDockPlacementInvariantTests : IDisposable
{
    private static PresentationGrammarTokens DefaultGrammar() =>
        PresentationGrammarTokens.FromSettings("()", " ", "+", "P", "F", "M");

    public SolutionExplorerDockPlacementInvariantTests() =>
        InstrumentPlacementRuntime.ResetToCodeDefaults();

    public void Dispose() =>
        InstrumentPlacementRuntime.ResetToCodeDefaults();

    private static MainWindowHostSurfaceFrame Compose(
        string presentationLine,
        bool intentSolutionExplorerVisible,
        DisplaySettings display)
    {
        var parse = PresentationParser.Parse(presentationLine, DefaultGrammar());
        Assert.True(parse.IsSuccess);

        return MainWindowHostSurfaceCompositor.ComposeFrame(
            new MainWindowShellSurfaceCompositionInput(
                parse,
                IntentSolutionExplorerVisible: intentSolutionExplorerVisible,
                IntentChatPanelExpanded: false,
                SuppressPfdColumnForPfdHostWindow: false,
                SuppressMfdColumnForMfdHostWindow: false,
                ExpandedMfdWidthPixels: 300,
                CollapsedMfdWidthPixels: 12,
                DisplaySettings: display,
                SafetyLevel: "L2"));
    }

    private static string? InstrumentInSlot(MainWindowHostSurfaceFrame frame, string slotId)
    {
        foreach (var i in frame.Instruments)
        {
            if (string.Equals(i.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
                return i.InstrumentId;
        }

        return null;
    }

    [Fact]
    public void DefaultRouting_tree_in_pfd_slot_matches_frame_and_secondary_shell_is_not_the_tree_host()
    {
        var display = new DisplaySettings();
        var frame = Compose("(P+F) (M)", intentSolutionExplorerVisible: true, display);

        Assert.True(MainWindowDockedGridInstrumentSlots.IsDockedPfdSolutionExplorerTree(display));
        Assert.False(MainWindowDockedGridInstrumentSlots.IsDockedMfdSolutionExplorerTree(display));

        Assert.Equal(CockpitStandardInstrumentIds.SolutionExplorerTree, InstrumentInSlot(frame, CockpitSlotIds.Pfd));
        Assert.Null(InstrumentInSlot(frame, CockpitSlotIds.Mfd));
    }

    [Fact]
    public void Pfd_workspace_map_mfd_tree_secondary_shell_can_show_solution_explorer_page()
    {
        var display = new DisplaySettings
        {
            Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map",
                [InstrumentRoutingSlotKeys.MfdPrimary] = "solution_explorer_tree",
            },
        };

        // Якорь M на первом экране — иначе (P+F) (M) даёт MfdColumnVisibleInMainGrid=false и слот MFD в main не монтируется.
        var frame = Compose("(P+F+M)", intentSolutionExplorerVisible: true, display);

        Assert.False(MainWindowDockedGridInstrumentSlots.IsDockedPfdSolutionExplorerTree(display));
        Assert.True(MainWindowDockedGridInstrumentSlots.IsDockedMfdSolutionExplorerTree(display));

        Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, InstrumentInSlot(frame, CockpitSlotIds.Pfd));
        Assert.Equal(CockpitStandardInstrumentIds.SolutionExplorerTree, InstrumentInSlot(frame, CockpitSlotIds.Mfd));
    }

    [Fact]
    public void Frame_instruments_agree_with_resolved_slot_ids_for_each_slot()
    {
        var display = new DisplaySettings
        {
            Instruments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map",
            },
            PreferRepoInstruments = false,
        };

        var frame = Compose("(P+F) (M)", intentSolutionExplorerVisible: true, display);

        var pfdResolved = MainWindowDockedGridInstrumentSlots.ResolvePfdInstrumentId(display);
        var mfdResolved = MainWindowDockedGridInstrumentSlots.ResolveMfdInstrumentId(display);

        if (frame.Shell.PfdSurfaceVisible)
        {
            var inFrame = InstrumentInSlot(frame, CockpitSlotIds.Pfd);
            if (inFrame is not null)
                Assert.Equal(pfdResolved, inFrame);
        }

        if (frame.Shell.MfdColumnVisibleInMainGrid)
        {
            var inFrame = InstrumentInSlot(frame, CockpitSlotIds.Mfd);
            if (inFrame is not null)
                Assert.Equal(mfdResolved, inFrame);
        }
    }

    [Fact]
    public void Workspace_routing_alias_prefers_repo_when_flag_set_compositor_and_flags_align()
    {
        try
        {
            InstrumentPlacementRuntime.ApplyWorkspaceInstrumentRouting(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map",
                });

            var display = new DisplaySettings { PreferRepoInstruments = true };
            var frame = Compose("(P+F) (M)", intentSolutionExplorerVisible: true, display);

            Assert.False(MainWindowDockedGridInstrumentSlots.IsDockedPfdSolutionExplorerTree(display));
            Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, InstrumentInSlot(frame, CockpitSlotIds.Pfd));
        }
        finally
        {
            InstrumentPlacementRuntime.ResetToCodeDefaults();
        }
    }
}
