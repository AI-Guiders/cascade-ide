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
            buildLine: "Build: idle",
            buildShort: "READY",
            isBuildRunning: false,
            testsLine: "Tests: x",
            testsShort: "imp 0",
            debugLine: "Debug: idle",
            debugShort: "DBG · —",
            gitLine: "Git: 0 staged",
            gitShort: "main · Δ0");

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
            "Build: running…",
            "BUILD…",
            isBuildRunning: true,
            "Tests: a",
            "a",
            "Debug: idle",
            "—",
            "Git: a",
            "a");

        Assert.True(col[0].IsBuildRunning);
        Assert.True(col[0].IsBuildSource);
        Assert.False(col[1].IsBuildSource);
    }
}
