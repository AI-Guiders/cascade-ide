using System.Collections.ObjectModel;
using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceHealthSurfaceCompositorTests
{
    private static readonly IWorkspaceHealthSurfaceCompositor SurfaceCompositor = new WorkspaceHealthSurfaceCompositor();

    [Fact]
    public void Rebuild_fills_four_segments_in_order_build_tests_debug_git()
    {
        var col = new ObservableCollection<WorkspaceHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new WorkspaceHealthInputSnapshot(
                Build: new WorkspaceHealthSegmentInput("Build: idle", "READY", IsBuildRunning: false),
                Tests: new WorkspaceHealthSegmentInput("Tests: x", "imp 0"),
                Debug: new WorkspaceHealthSegmentInput("Debug: idle", "DBG · —"),
                Git: new WorkspaceHealthSegmentInput("Git: 0 staged", "main · Δ0")),
            decision: new WorkspaceHealthSurfaceDecision(Enabled: true));

        Assert.Equal(4, col.Count);
        Assert.Equal(WorkspaceHealthSource.Build, col[0].Source);
        Assert.Equal("Build: idle", col[0].LineText);
        Assert.False(col[0].IsBuildRunning);

        Assert.Equal(WorkspaceHealthSource.Tests, col[1].Source);
        Assert.Equal(WorkspaceHealthSource.Debug, col[2].Source);
        Assert.Equal(WorkspaceHealthSource.Git, col[3].Source);
    }

    [Fact]
    public void Instrument_deck_descriptor_matches_compositor_segment_order()
    {
        var deck = WorkspaceHealthInstrumentDeck.Default;
        Assert.Equal(WorkspaceHealthInstrumentDeck.DeckId, deck.DeckId);
        Assert.Equal(WorkspaceHealthInstrumentDeck.SemanticAnchorId, deck.SemanticAnchorId);
        Assert.Equal(InstrumentDeckLayoutPattern.Grid, deck.LayoutPattern);
        Assert.Equal(4, deck.OrderedInstrumentIds.Count);
        Assert.Equal(WorkspaceHealthSegmentIds.Build, deck.OrderedInstrumentIds[0]);
        Assert.Equal(WorkspaceHealthSegmentIds.Tests, deck.OrderedInstrumentIds[1]);
        Assert.Equal(WorkspaceHealthSegmentIds.Debug, deck.OrderedInstrumentIds[2]);
        Assert.Equal(WorkspaceHealthSegmentIds.Git, deck.OrderedInstrumentIds[3]);
        Assert.Same(WorkspaceHealthInstrumentDeck.OrderedSegmentIds, deck.OrderedInstrumentIds);
    }

    [Fact]
    public void Rebuild_sets_IsBuildRunning_on_build_segment()
    {
        var col = new ObservableCollection<WorkspaceHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new WorkspaceHealthInputSnapshot(
                Build: new WorkspaceHealthSegmentInput("Build: running…", "BUILD…", IsBuildRunning: true),
                Tests: new WorkspaceHealthSegmentInput("Tests: a", "a"),
                Debug: new WorkspaceHealthSegmentInput("Debug: idle", "—"),
                Git: new WorkspaceHealthSegmentInput("Git: a", "a")),
            decision: new WorkspaceHealthSurfaceDecision(Enabled: true));

        Assert.True(col[0].IsBuildRunning);
        Assert.True(col[0].IsBuildSource);
        Assert.False(col[1].IsBuildSource);
    }

    [Fact]
    public void Rebuild_ignores_IsBuildRunning_on_non_build_segments()
    {
        var col = new ObservableCollection<WorkspaceHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new WorkspaceHealthInputSnapshot(
                Build: new WorkspaceHealthSegmentInput("Build: idle", "READY", IsBuildRunning: false),
                Tests: new WorkspaceHealthSegmentInput("Tests: x", "t", IsBuildRunning: true),
                Debug: new WorkspaceHealthSegmentInput("Debug: idle", "d"),
                Git: new WorkspaceHealthSegmentInput("Git: a", "g")),
            decision: new WorkspaceHealthSurfaceDecision(Enabled: true));

        Assert.False(col[1].IsBuildRunning);
        Assert.False(col[2].IsBuildRunning);
        Assert.False(col[3].IsBuildRunning);
    }
}
