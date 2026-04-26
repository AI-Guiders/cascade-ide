using System.Collections.Generic;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthSnapshotUnitTests
{
    [Fact]
    public void Build_uses_solution_scope_when_startup_project_has_no_active_signal()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new StartupProjectPathChanged("src/App/App.csproj"));
        bus.Publish(new GitStateChanged("Git: clean", "main"));

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Solution, snapshot.Solution.Build.Scope);
        Assert.Equal(IdeHealthScope.Solution, snapshot.Solution.Tests.Scope);
        Assert.Equal(IdeHealthScope.Solution, snapshot.Solution.Debug.Scope);
        Assert.Null(snapshot.Solution.Build.ProjectPath);
        Assert.Null(snapshot.Solution.Tests.ProjectPath);
        Assert.Null(snapshot.Solution.Debug.ProjectPath);
    }

    [Fact]
    public void Build_uses_project_scope_when_solution_signal_is_active()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new StartupProjectPathChanged("src/App/App.csproj"));
        bus.Publish(new GitStateChanged("Git: clean", "main"));
        bus.Publish(new BuildStateChanged(true));

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Project, snapshot.Solution.Build.Scope);
        Assert.Equal(IdeHealthScope.Project, snapshot.Solution.Tests.Scope);
        Assert.Equal(IdeHealthScope.Project, snapshot.Solution.Debug.Scope);
        Assert.Equal("src/App/App.csproj", snapshot.Solution.Build.ProjectPath);
        Assert.Equal("src/App/App.csproj", snapshot.Solution.Tests.ProjectPath);
        Assert.Equal("src/App/App.csproj", snapshot.Solution.Debug.ProjectPath);
    }

    [Fact]
    public void Build_reflects_IdeHostStateChanged_for_Lsp_hint()
    {
        var bus = new InMemoryDataBus(asynchronousDispatch: false);
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new GitStateChanged("Git: clean", "main"));
        bus.Publish(new IdeHostStateChanged(
            CSharpLspProcessActive: true,
            MarkdownLspProcessActive: false,
            CSharpLspHostPresent: true,
            MarkdownLspHostPresent: false));

        var snapshot = unit.Build(default);

        Assert.Equal("LSP · C#", snapshot.IdeHost.LspStatusHint);
    }

    [Fact]
    public void Build_uses_git_segment_unit_output()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new GitStateChanged("Git: dirty +2/-1", "main*"));

        var snapshot = unit.Build(default);

        Assert.Equal("Git: dirty +2/-1", snapshot.Workspace.Git.LineText);
        Assert.Equal("main*", snapshot.Workspace.Git.CockpitShort);
        Assert.Equal(IdeHealthStratum.Workspace, snapshot.Workspace.Git.Stratum);
    }

    [Fact]
    public void Build_reflects_last_build_result_after_finish_event()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new StartupProjectPathChanged("src/App/App.csproj"));
        bus.Publish(new GitStateChanged("Git: clean", "main"));
        bus.Publish(new BuildStateChanged(true));
        bus.Publish(new BuildStateChanged(false, 0, true));

        var snapshot = unit.Build(default);

        Assert.Contains("last OK", snapshot.Solution.Build.LineText, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_reads_build_signal_from_databus()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new StartupProjectPathChanged("src/App/App.csproj"));
        bus.Publish(new GitStateChanged("Git: clean", "main"));
        bus.Publish(new BuildStateChanged(true));

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Project, snapshot.Solution.Build.Scope);
        Assert.True(snapshot.Solution.Build.IsBuildRunning);
        Assert.Equal("BUILD…", snapshot.Solution.Build.CockpitShort);
    }

    [Fact]
    public void Build_reads_tests_signal_from_databus()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new StartupProjectPathChanged("src/App/App.csproj"));
        bus.Publish(new GitStateChanged("Git: clean", "main"));
        bus.Publish(new TestsStateChanged("7/7 passed, 0 failed", 0));

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Project, snapshot.Solution.Tests.Scope);
        Assert.Equal("src/App/App.csproj", snapshot.Solution.Tests.ProjectPath);
        Assert.Contains("7/7 passed, 0 failed", snapshot.Solution.Tests.LineText, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_reads_debug_signal_from_databus()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        bus.Publish(new StartupProjectPathChanged("src/App/App.csproj"));
        bus.Publish(new GitStateChanged("Git: clean", "main"));

        var paused = DebugSessionSnapshot.Empty with
        {
            HasActiveSession = true,
            IsExecutionStopped = true,
            StackFrames = new (string Name, string? File, int Line)[] { ("A", "a.cs", 1) },
            VariableRootScopes = new[]
            {
                new DebugVariableRootScope("Locals", new List<DebugVariableRow> { new("x", "1", "int") })
            }
        };
        bus.Publish(new BuildStateChanged(true));
        bus.Publish(new DebugStateChanged(paused));

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Project, snapshot.Solution.Debug.Scope);
        Assert.Contains("paused", snapshot.Solution.Debug.LineText, StringComparison.Ordinal);
    }

    [Fact]
    public void Dispose_makes_subsequent_Build_throw()
    {
        var bus = new InMemoryDataBus();
        var unit = new IdeHealthSnapshotUnit(bus);
        unit.Dispose();
        Assert.Throws<ObjectDisposedException>(() => unit.Build(default));
    }
}
