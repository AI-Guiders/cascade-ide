using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Cockpit.Composition.Shell;
using CascadeIDE.Models;
using CascadeIDE.Services.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Регрессии вида «открыли решение — дерево не там»: одна карта инструментов (<see cref="InstrumentPlacementRuntime"/> + <see cref="DisplaySettings"/>)
/// должна давать тот же смысл, что и кадр композитора и что и флаги привязки в <c>MainWindowViewModel.DockInstrumentSlots</c>.
/// Логика разрешения слотов ниже должна совпадать с приватными методами в <c>MainWindowViewModel</c> (surface DockedGrid).
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

    /// <summary>Копия <see cref="MainWindowViewModel.ResolveDockedPfdInstrumentId"/> / Mfd для surface DockedGrid.</summary>
    private static string ResolveDockedPfdInstrumentId(DisplaySettings display)
    {
        if (InstrumentPlacementRuntime.TryResolveInstrument(
                MainWindowHostSurfaceIds.DockedGrid,
                CockpitSlotIds.Pfd,
                display,
                out var id)
            && !string.IsNullOrWhiteSpace(id))
            return id;

        return CockpitStandardInstrumentIds.SolutionExplorerTree;
    }

    private static string ResolveDockedMfdInstrumentId(DisplaySettings display)
    {
        if (InstrumentPlacementRuntime.TryResolveInstrument(
                MainWindowHostSurfaceIds.DockedGrid,
                CockpitSlotIds.Mfd,
                display,
                out var id)
            && !string.IsNullOrWhiteSpace(id))
            return id;

        return "";
    }

    private static bool IsDockedPfdSolutionExplorerTree(DisplaySettings d) =>
        string.Equals(
            ResolveDockedPfdInstrumentId(d),
            CockpitStandardInstrumentIds.SolutionExplorerTree,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsDockedMfdSolutionExplorerTree(DisplaySettings d) =>
        string.Equals(
            ResolveDockedMfdInstrumentId(d),
            CockpitStandardInstrumentIds.SolutionExplorerTree,
            StringComparison.OrdinalIgnoreCase)
        && !IsDockedPfdSolutionExplorerTree(d);

    /// <summary>Страница «Обозреватель» на вторичном контуре разрешена только если дерево в слоте MFD (см. <c>IsSecondaryShellPageAllowed</c>).</summary>
    private static bool SecondaryShellSolutionExplorerPageAllowed(DisplaySettings d) =>
        IsDockedMfdSolutionExplorerTree(d);

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

        Assert.True(IsDockedPfdSolutionExplorerTree(display));
        Assert.False(IsDockedMfdSolutionExplorerTree(display));
        Assert.False(SecondaryShellSolutionExplorerPageAllowed(display));

        Assert.Equal(CockpitStandardInstrumentIds.SolutionExplorerTree, InstrumentInSlot(frame, CockpitSlotIds.Pfd));
        Assert.Null(InstrumentInSlot(frame, CockpitSlotIds.Mfd));
    }

    [Fact]
    public void Pfd_workspace_map_mfd_tree_secondary_shell_can_show_solution_explorer_page()
    {
        var display = new DisplaySettings
        {
            InstrumentRouting = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map",
                [InstrumentRoutingSlotKeys.MfdPrimary] = "solution_explorer_tree",
            },
        };

        // Якорь M на первом экране — иначе (P+F) (M) даёт MfdColumnVisibleInMainGrid=false и слот MFD в main не монтируется.
        var frame = Compose("(P+F+M)", intentSolutionExplorerVisible: true, display);

        Assert.False(IsDockedPfdSolutionExplorerTree(display));
        Assert.True(IsDockedMfdSolutionExplorerTree(display));
        Assert.True(SecondaryShellSolutionExplorerPageAllowed(display));

        Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, InstrumentInSlot(frame, CockpitSlotIds.Pfd));
        Assert.Equal(CockpitStandardInstrumentIds.SolutionExplorerTree, InstrumentInSlot(frame, CockpitSlotIds.Mfd));
    }

    [Fact]
    public void Frame_instruments_agree_with_resolved_slot_ids_for_each_slot()
    {
        var display = new DisplaySettings
        {
            InstrumentRouting = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [InstrumentRoutingSlotKeys.PfdPrimary] = "workspace_map",
            },
            PreferRepoInstrumentsPlacement = false,
        };

        var frame = Compose("(P+F) (M)", intentSolutionExplorerVisible: true, display);

        var pfdResolved = ResolveDockedPfdInstrumentId(display);
        var mfdResolved = ResolveDockedMfdInstrumentId(display);

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

            var display = new DisplaySettings { PreferRepoInstrumentsPlacement = true };
            var frame = Compose("(P+F) (M)", intentSolutionExplorerVisible: true, display);

            Assert.False(IsDockedPfdSolutionExplorerTree(display));
            Assert.Equal(CockpitStandardInstrumentIds.WorkspaceNavigationMap, InstrumentInSlot(frame, CockpitSlotIds.Pfd));
        }
        finally
        {
            InstrumentPlacementRuntime.ResetToCodeDefaults();
        }
    }
}
