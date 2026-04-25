using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthSnapshotUnitTests
{
    [Fact]
    public void Build_uses_solution_scope_when_startup_project_has_no_active_signal()
    {
        var unit = new IdeHealthSnapshotUnit(
            isBuilding: () => false,
            lastTestSummary: () => "",
            impactedTestsBadge: () => 0,
            startupProjectPath: () => "src/App/App.csproj",
            dapDebug: new IdeDapDebugSession(null),
            workspaceHealthGitLine: () => "Git: clean",
            workspaceHealthGitCockpitShort: () => "main");

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Solution, snapshot.Build.Scope);
        Assert.Equal(IdeHealthScope.Solution, snapshot.Tests.Scope);
        Assert.Equal(IdeHealthScope.Solution, snapshot.Debug.Scope);
        Assert.Null(snapshot.Build.ProjectPath);
        Assert.Null(snapshot.Tests.ProjectPath);
        Assert.Null(snapshot.Debug.ProjectPath);
    }

    [Fact]
    public void Build_uses_project_scope_when_solution_signal_is_active()
    {
        var unit = new IdeHealthSnapshotUnit(
            isBuilding: () => true,
            lastTestSummary: () => "",
            impactedTestsBadge: () => 0,
            startupProjectPath: () => "src/App/App.csproj",
            dapDebug: new IdeDapDebugSession(null),
            workspaceHealthGitLine: () => "Git: clean",
            workspaceHealthGitCockpitShort: () => "main");

        var snapshot = unit.Build(default);

        Assert.Equal(IdeHealthScope.Project, snapshot.Build.Scope);
        Assert.Equal(IdeHealthScope.Project, snapshot.Tests.Scope);
        Assert.Equal(IdeHealthScope.Project, snapshot.Debug.Scope);
        Assert.Equal("src/App/App.csproj", snapshot.Build.ProjectPath);
        Assert.Equal("src/App/App.csproj", snapshot.Tests.ProjectPath);
        Assert.Equal("src/App/App.csproj", snapshot.Debug.ProjectPath);
    }
}
