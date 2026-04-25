using System.Collections.ObjectModel;
using CascadeIDE.Cockpit;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceHealthSurfaceCompositorTests
{
    private static readonly IIdeHealthSurfaceCompositor SurfaceCompositor = new IdeHealthSurfaceCompositor();

    [Fact]
    public void Rebuild_fills_four_segments_in_order_build_tests_debug_git()
    {
        var col = new ObservableCollection<IdeHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new IdeHealthInputSnapshot(
                Build: new IdeHealthSegmentInput("Build: idle", "READY", IsBuildRunning: false, Stratum: IdeHealthStratum.Solution),
                Tests: new IdeHealthSegmentInput("Tests: x", "imp 0", Stratum: IdeHealthStratum.Solution),
                Debug: new IdeHealthSegmentInput("Debug: idle", "DBG · —", Stratum: IdeHealthStratum.Solution),
                Git: new IdeHealthSegmentInput("Git: 0 staged", "main · Δ0", Stratum: IdeHealthStratum.Workspace)),
            decision: new IdeHealthSurfaceDecision(Enabled: true));

        Assert.Equal(4, col.Count);
        Assert.Equal(IdeHealthSource.Build, col[0].Source);
        Assert.Equal("Build: idle", col[0].LineText);
        Assert.False(col[0].IsBuildRunning);

        Assert.Equal(IdeHealthSource.Tests, col[1].Source);
        Assert.Equal(IdeHealthSource.Debug, col[2].Source);
        Assert.Equal(IdeHealthSource.Git, col[3].Source);
        Assert.Equal(IdeHealthStratum.Solution, col[0].Stratum);
        Assert.Equal(IdeHealthStratum.Solution, col[1].Stratum);
        Assert.Equal(IdeHealthStratum.Solution, col[2].Stratum);
        Assert.Equal(IdeHealthStratum.Workspace, col[3].Stratum);
        Assert.Equal(IdeHealthScope.Solution, col[0].Scope);
        Assert.Equal(IdeHealthScope.Solution, col[1].Scope);
        Assert.Equal(IdeHealthScope.Solution, col[2].Scope);
        Assert.Equal(IdeHealthScope.Solution, col[3].Scope);
        Assert.Null(col[2].ProjectPath);
    }

    [Fact]
    public void Instrument_deck_descriptor_matches_compositor_segment_order()
    {
        var deck = IdeHealthInstrumentDeck.Default;
        Assert.Equal(IdeHealthInstrumentDeck.DeckId, deck.DeckId);
        Assert.Equal(IdeHealthInstrumentDeck.SemanticAnchorId, deck.SemanticAnchorId);
        Assert.Equal(InstrumentDeckLayoutPattern.Grid, deck.LayoutPattern);
        Assert.Equal(4, deck.OrderedInstrumentIds.Count);
        Assert.Equal(IdeHealthSegmentIds.Build, deck.OrderedInstrumentIds[0]);
        Assert.Equal(IdeHealthSegmentIds.Tests, deck.OrderedInstrumentIds[1]);
        Assert.Equal(IdeHealthSegmentIds.Debug, deck.OrderedInstrumentIds[2]);
        Assert.Equal(IdeHealthSegmentIds.Git, deck.OrderedInstrumentIds[3]);
        Assert.Same(IdeHealthInstrumentDeck.OrderedSegmentIds, deck.OrderedInstrumentIds);
    }

    [Fact]
    public void Rebuild_sets_IsBuildRunning_on_build_segment()
    {
        var col = new ObservableCollection<IdeHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new IdeHealthInputSnapshot(
                Build: new IdeHealthSegmentInput("Build: running…", "BUILD…", IsBuildRunning: true, Stratum: IdeHealthStratum.Solution),
                Tests: new IdeHealthSegmentInput("Tests: a", "a"),
                Debug: new IdeHealthSegmentInput("Debug: idle", "—", Stratum: IdeHealthStratum.Solution),
                Git: new IdeHealthSegmentInput("Git: a", "a", Stratum: IdeHealthStratum.Workspace)),
            decision: new IdeHealthSurfaceDecision(Enabled: true));

        Assert.True(col[0].IsBuildRunning);
        Assert.True(col[0].IsBuildSource);
        Assert.False(col[1].IsBuildSource);
    }

    [Fact]
    public void Rebuild_ignores_IsBuildRunning_on_non_build_segments()
    {
        var col = new ObservableCollection<IdeHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new IdeHealthInputSnapshot(
                Build: new IdeHealthSegmentInput("Build: idle", "READY", IsBuildRunning: false, Stratum: IdeHealthStratum.Solution),
                Tests: new IdeHealthSegmentInput("Tests: x", "t", IsBuildRunning: true),
                Debug: new IdeHealthSegmentInput("Debug: idle", "d", Stratum: IdeHealthStratum.Solution),
                Git: new IdeHealthSegmentInput("Git: a", "g", Stratum: IdeHealthStratum.Workspace)),
            decision: new IdeHealthSurfaceDecision(Enabled: true));

        Assert.False(col[1].IsBuildRunning);
        Assert.False(col[2].IsBuildRunning);
        Assert.False(col[3].IsBuildRunning);
    }

    [Fact]
    public void Rebuild_keeps_project_scope_and_project_path()
    {
        var col = new ObservableCollection<IdeHealthSegment>();
        SurfaceCompositor.Compose(
            col,
            payload: new IdeHealthInputSnapshot(
                Build: IdeHealthFormattingUnit.Default.ProjectBuildSegment("src/App/App.csproj", isBuilding: false),
                Tests: new IdeHealthSegmentInput("Tests: x", "t"),
                Debug: new IdeHealthSegmentInput("Debug: idle", "d"),
                Git: new IdeHealthSegmentInput("Git: a", "g", Stratum: IdeHealthStratum.Workspace)),
            decision: new IdeHealthSurfaceDecision(Enabled: true));

        Assert.Equal(IdeHealthScope.Project, col[0].Scope);
        Assert.Equal("src/App/App.csproj", col[0].ProjectPath);
        Assert.Equal(IdeHealthScope.Solution, col[1].Scope);
        Assert.Null(col[1].ProjectPath);
    }
}
