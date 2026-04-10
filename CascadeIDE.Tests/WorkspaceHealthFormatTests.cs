using CascadeIDE.Features.UiChrome;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceHealthFormatTests
{
    [Fact]
    public void BuildSegment_idle_and_running()
    {
        var idle = WorkspaceHealthFormat.BuildSegment(isBuilding: false);
        Assert.Equal("Build: idle", idle.LineText);
        Assert.Equal("READY", idle.CockpitShort);
        Assert.False(idle.IsBuildRunning);

        var run = WorkspaceHealthFormat.BuildSegment(isBuilding: true);
        Assert.Equal("Build: running…", run.LineText);
        Assert.Equal("BUILD…", run.CockpitShort);
        Assert.True(run.IsBuildRunning);
    }

    [Fact]
    public void TestsSegment_uses_summary_or_impacted()
    {
        var withSummary = WorkspaceHealthFormat.TestsSegment("ok 3 passed", 0);
        Assert.Equal("Tests: ok 3 passed", withSummary.LineText);
        Assert.Equal("ok 3 passed", withSummary.CockpitShort);

        var impacted = WorkspaceHealthFormat.TestsSegment(null, 5);
        Assert.Equal("Tests: impacted 5", impacted.LineText);
        Assert.Equal("imp 5", impacted.CockpitShort);
    }

    [Fact]
    public void TestsSegment_truncates_long_summary_in_cockpit()
    {
        var longText = new string('a', 40);
        var s = WorkspaceHealthFormat.TestsSegment(longText, 0);
        Assert.Equal(34, s.CockpitShort.Length);
        Assert.EndsWith("…", s.CockpitShort);
    }

    [Fact]
    public void DebugSegment_idle_paused_running()
    {
        var idle = WorkspaceHealthFormat.DebugSegment(false, false, 0, 0);
        Assert.Equal("Debug: idle", idle.LineText);
        Assert.Equal("DBG · —", idle.CockpitShort);

        var paused = WorkspaceHealthFormat.DebugSegment(true, true, 2, 4);
        Assert.Contains("frames 2", paused.LineText);
        Assert.Contains("vars 4", paused.LineText);
        Assert.Equal("DBG · pause · 2fr", paused.CockpitShort);

        var running = WorkspaceHealthFormat.DebugSegment(true, false, 0, 0);
        Assert.Equal("Debug: running…", running.LineText);
        Assert.Equal("DBG · run", running.CockpitShort);
    }

    [Fact]
    public void Compose_fills_all_four_segments()
    {
        var snap = WorkspaceHealthFormat.Compose(
            isBuilding: false,
            lastTestSummary: "",
            impactedTestsBadge: 1,
            hasDebugSession: false,
            debugExecutionStopped: false,
            debugStackFrameCount: 0,
            debugVariableCount: 0,
            gitLine: "Git: 0 staged",
            gitCockpitShort: "main · Δ0");

        Assert.Equal("Build: idle", snap.Build.LineText);
        Assert.Equal("Tests: impacted 1", snap.Tests.LineText);
        Assert.Equal("Debug: idle", snap.Debug.LineText);
        Assert.Equal("Git: 0 staged", snap.Git.LineText);
        Assert.Equal("main · Δ0", snap.Git.CockpitShort);
    }
}
