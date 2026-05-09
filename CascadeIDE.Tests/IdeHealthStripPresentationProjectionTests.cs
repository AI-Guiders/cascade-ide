using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Features.Shell.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthStripPresentationProjectionTests
{
    private static IdeHealthInputSnapshot Snapshot()
        => IdeHealthInputSnapshot.FromFlat(
            build: new IdeHealthSegmentInput("build-line", "B"),
            tests: new IdeHealthSegmentInput("tests-line", "T"),
            debug: new IdeHealthSegmentInput("debug-line", "D"),
            git: new IdeHealthSegmentInput("git-line", "G"));

    [Fact]
    public void Nullable_snapshot_returns_empty_strings()
    {
        IdeHealthInputSnapshot? nullSnap = null;
        Assert.Equal("", IdeHealthStripPresentationProjection.SolutionBuildLineText(nullSnap));
        Assert.Equal("", IdeHealthStripPresentationProjection.SolutionBuildCockpitShort(nullSnap));
        Assert.Equal("", IdeHealthStripPresentationProjection.SolutionTestsLineText(nullSnap));
        Assert.Equal("", IdeHealthStripPresentationProjection.SolutionTestsCockpitShort(nullSnap));
        Assert.Equal("", IdeHealthStripPresentationProjection.SolutionDebugLineText(nullSnap));
        Assert.Equal("", IdeHealthStripPresentationProjection.SolutionDebugCockpitShort(nullSnap));
    }

    [Fact]
    public void Snapshotted_solution_segments_project_line_and_short()
    {
        var s = Snapshot();
        Assert.Equal("build-line", IdeHealthStripPresentationProjection.SolutionBuildLineText(s));
        Assert.Equal("B", IdeHealthStripPresentationProjection.SolutionBuildCockpitShort(s));
        Assert.Equal("tests-line", IdeHealthStripPresentationProjection.SolutionTestsLineText(s));
        Assert.Equal("T", IdeHealthStripPresentationProjection.SolutionTestsCockpitShort(s));
        Assert.Equal("debug-line", IdeHealthStripPresentationProjection.SolutionDebugLineText(s));
        Assert.Equal("D", IdeHealthStripPresentationProjection.SolutionDebugCockpitShort(s));
    }
}
