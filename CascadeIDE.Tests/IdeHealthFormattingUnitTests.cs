using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthFormattingUnitTests
{
    [Fact]
    public void BuildSegment_idle_and_running()
    {
        var idle = IdeHealthFormattingUnit.Default.BuildSegment(isBuilding: false);
        Assert.Equal("Build: idle", idle.LineText);
        Assert.Equal("READY", idle.CockpitShort);
        Assert.False(idle.IsBuildRunning);
        Assert.Equal(IdeHealthStratum.Solution, idle.Stratum);
        Assert.Equal(IdeHealthScope.Solution, idle.Scope);
        Assert.Null(idle.ProjectPath);

        var run = IdeHealthFormattingUnit.Default.BuildSegment(isBuilding: true);
        Assert.Equal("Build: running…", run.LineText);
        Assert.Equal("BUILD…", run.CockpitShort);
        Assert.True(run.IsBuildRunning);
        Assert.Equal(IdeHealthStratum.Solution, run.Stratum);
        Assert.Equal(IdeHealthScope.Solution, run.Scope);
        Assert.Null(run.ProjectPath);
    }

    [Fact]
    public void TestsSegment_uses_summary_or_impacted()
    {
        var withSummary = IdeHealthFormattingUnit.Default.TestsSegment("ok 3 passed", 0);
        Assert.Equal("Tests: ok 3 passed", withSummary.LineText);
        Assert.Equal("ok 3 passed", withSummary.CockpitShort);
        Assert.Equal(IdeHealthStratum.Solution, withSummary.Stratum);
        Assert.Equal(IdeHealthScope.Solution, withSummary.Scope);
        Assert.Null(withSummary.ProjectPath);

        var impacted = IdeHealthFormattingUnit.Default.TestsSegment(null, 5);
        Assert.Equal("Tests: impacted 5", impacted.LineText);
        Assert.Equal("imp 5", impacted.CockpitShort);
        Assert.Equal(IdeHealthStratum.Solution, impacted.Stratum);
        Assert.Equal(IdeHealthScope.Solution, impacted.Scope);
        Assert.Null(impacted.ProjectPath);
    }

    [Fact]
    public void TestsSegment_truncates_long_summary_in_cockpit()
    {
        var longText = new string('a', 40);
        var s = IdeHealthFormattingUnit.Default.TestsSegment(longText, 0);
        Assert.Equal(34, s.CockpitShort.Length);
        Assert.EndsWith("…", s.CockpitShort);
    }

    [Fact]
    public void DebugSegment_idle_paused_running()
    {
        var idle = IdeHealthFormattingUnit.Default.DebugSegment(false, false, 0, 0);
        Assert.Equal("Debug: idle", idle.LineText);
        Assert.Equal("DBG · —", idle.CockpitShort);
        Assert.Equal(IdeHealthStratum.Solution, idle.Stratum);
        Assert.Equal(IdeHealthScope.Solution, idle.Scope);

        var paused = IdeHealthFormattingUnit.Default.DebugSegment(true, true, 2, 4);
        Assert.Contains("frames 2", paused.LineText);
        Assert.Contains("vars 4", paused.LineText);
        Assert.Equal("DBG · pause · 2fr", paused.CockpitShort);
        Assert.Equal(IdeHealthStratum.Solution, paused.Stratum);
        Assert.Equal(IdeHealthScope.Solution, paused.Scope);

        var running = IdeHealthFormattingUnit.Default.DebugSegment(true, false, 0, 0);
        Assert.Equal("Debug: running…", running.LineText);
        Assert.Equal("DBG · run", running.CockpitShort);
        Assert.Equal(IdeHealthStratum.Solution, running.Stratum);
        Assert.Equal(IdeHealthScope.Solution, running.Scope);
    }

    [Fact]
    public void Compose_fills_all_four_segments()
    {
        var snap = IdeHealthFormattingUnit.Default.Compose(
            buildState: BuildStateSnapshot.Empty,
            lastTestSummary: "",
            impactedTestsBadge: 1,
            hasDebugSession: false,
            debugExecutionStopped: false,
            debugStackFrameCount: 0,
            debugVariableCount: 0,
            gitLine: "Git: 0 staged",
            gitCockpitShort: "main · Δ0");

        Assert.Equal("Build: idle", snap.Solution.Build.LineText);
        Assert.Equal("Tests: impacted 1", snap.Solution.Tests.LineText);
        Assert.Equal("Debug: idle", snap.Solution.Debug.LineText);
        Assert.Equal("Git: 0 staged", snap.Workspace.Git.LineText);
        Assert.Equal("main · Δ0", snap.Workspace.Git.CockpitShort);
        Assert.Equal(IdeHealthStratum.Solution, snap.Solution.Build.Stratum);
        Assert.Equal(IdeHealthStratum.Solution, snap.Solution.Tests.Stratum);
        Assert.Equal(IdeHealthStratum.Solution, snap.Solution.Debug.Stratum);
        Assert.Equal(IdeHealthStratum.Workspace, snap.Workspace.Git.Stratum);
        Assert.Equal(IdeHealthScope.Solution, snap.Solution.Build.Scope);
        Assert.Equal(IdeHealthScope.Solution, snap.Solution.Tests.Scope);
        Assert.Equal(IdeHealthScope.Solution, snap.Solution.Debug.Scope);
        Assert.Equal(IdeHealthScope.Solution, snap.Workspace.Git.Scope);
        Assert.Equal(default(IdeHealthIdeHostInput), snap.IdeHost);
        Assert.Null(snap.IdeHost.LspStatusHint);
        Assert.Equal(snap, IdeHealthStrataComposer.Compose(snap.Workspace, snap.Solution, snap.IdeHost));
    }

    [Fact]
    public void IdeHealthFormattingUnit_Default_implements_ICockpitComputeUnit()
    {
        ICockpitComputeUnit unit = IdeHealthFormattingUnit.Default;
        Assert.NotNull(unit);
    }

    [Fact]
    public void Composed_snapshot_implements_ICockpitComputeUnitPayload()
    {
        var snap = IdeHealthFormattingUnit.Default.Compose(
            buildState: BuildStateSnapshot.Empty,
            lastTestSummary: null,
            impactedTestsBadge: 0,
            hasDebugSession: false,
            debugExecutionStopped: false,
            debugStackFrameCount: 0,
            debugVariableCount: 0,
            gitLine: "g",
            gitCockpitShort: "c");
        ICockpitComputeUnitPayload payload = snap;
        Assert.NotNull(payload);
    }

    [Fact]
    public void Project_segments_are_marked_as_project_scope()
    {
        var build = IdeHealthFormattingUnit.Default.ProjectBuildSegment("src/App/App.csproj", isBuilding: true);
        var tests = IdeHealthFormattingUnit.Default.ProjectTestsSegment("src/App/App.csproj", "failed 3", impactedTestsBadge: 0);
        var debug = IdeHealthFormattingUnit.Default.ProjectDebugSegment("src/App/App.csproj", "paused");

        Assert.Equal(IdeHealthScope.Project, build.Scope);
        Assert.Equal(IdeHealthScope.Project, tests.Scope);
        Assert.Equal(IdeHealthScope.Project, debug.Scope);
        Assert.Equal("src/App/App.csproj", build.ProjectPath);
        Assert.Equal("src/App/App.csproj", tests.ProjectPath);
        Assert.Equal("src/App/App.csproj", debug.ProjectPath);
        Assert.Equal(IdeHealthStratum.Solution, build.Stratum);
        Assert.Equal(IdeHealthStratum.Solution, tests.Stratum);
        Assert.Equal(IdeHealthStratum.Solution, debug.Stratum);
    }

    [Fact]
    public void Project_tests_segment_falls_back_to_impacted_when_summary_empty()
    {
        var tests = IdeHealthFormattingUnit.Default.ProjectTestsSegment("src/App/App.csproj", summary: "", impactedTestsBadge: 2);
        Assert.Contains("impacted 2", tests.LineText);
        Assert.Contains("impacted 2", tests.CockpitShort);
        Assert.Equal(IdeHealthScope.Project, tests.Scope);
        Assert.Equal("src/App/App.csproj", tests.ProjectPath);
    }
}
