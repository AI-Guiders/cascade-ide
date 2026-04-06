using System.Collections.ObjectModel;
using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AttentionStripCompositorTests
{
    [Fact]
    public void Rebuild_fills_four_segments_in_order_build_tests_debug_git()
    {
        var col = new ObservableCollection<AttentionStripSegment>();
        AttentionStripCompositor.Rebuild(
            col,
            new AttentionStripInputSnapshot(
                Build: new AttentionStripSegmentInput("Build: idle", "READY", IsBuildRunning: false),
                Tests: new AttentionStripSegmentInput("Tests: x", "imp 0"),
                Debug: new AttentionStripSegmentInput("Debug: idle", "DBG · —"),
                Git: new AttentionStripSegmentInput("Git: 0 staged", "main · Δ0")));

        Assert.Equal(4, col.Count);
        Assert.Equal(AttentionStripSource.Build, col[0].Source);
        Assert.Equal("Build: idle", col[0].LineText);
        Assert.False(col[0].IsBuildRunning);

        Assert.Equal(AttentionStripSource.Tests, col[1].Source);
        Assert.Equal(AttentionStripSource.Debug, col[2].Source);
        Assert.Equal(AttentionStripSource.Git, col[3].Source);
    }

    [Fact]
    public void Rebuild_sets_IsBuildRunning_on_build_segment()
    {
        var col = new ObservableCollection<AttentionStripSegment>();
        AttentionStripCompositor.Rebuild(
            col,
            new AttentionStripInputSnapshot(
                Build: new AttentionStripSegmentInput("Build: running…", "BUILD…", IsBuildRunning: true),
                Tests: new AttentionStripSegmentInput("Tests: a", "a"),
                Debug: new AttentionStripSegmentInput("Debug: idle", "—"),
                Git: new AttentionStripSegmentInput("Git: a", "a")));

        Assert.True(col[0].IsBuildRunning);
        Assert.True(col[0].IsBuildSource);
        Assert.False(col[1].IsBuildSource);
    }

    [Fact]
    public void Rebuild_ignores_IsBuildRunning_on_non_build_segments()
    {
        var col = new ObservableCollection<AttentionStripSegment>();
        AttentionStripCompositor.Rebuild(
            col,
            new AttentionStripInputSnapshot(
                Build: new AttentionStripSegmentInput("Build: idle", "READY", IsBuildRunning: false),
                Tests: new AttentionStripSegmentInput("Tests: x", "t", IsBuildRunning: true),
                Debug: new AttentionStripSegmentInput("Debug: idle", "d"),
                Git: new AttentionStripSegmentInput("Git: a", "g")));

        Assert.False(col[1].IsBuildRunning);
        Assert.False(col[2].IsBuildRunning);
        Assert.False(col[3].IsBuildRunning);
    }
}
