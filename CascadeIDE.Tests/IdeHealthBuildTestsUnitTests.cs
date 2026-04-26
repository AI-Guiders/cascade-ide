using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthBuildTestsUnitTests
{
    [Fact]
    public void Compose_returns_solution_segments_without_project_scope()
    {
        var decision = new IdeHealthScopeDecision(IdeHealthScope.Solution, ProjectPath: null);

        var snapshot = IdeHealthBuildTestsUnit.Default.Compose(
            decision,
            buildState: new BuildStateSnapshot(true),
            testSummary: "",
            impactedTestsBadge: 3);

        Assert.Equal("Build: running…", snapshot.Build.LineText);
        Assert.Equal(IdeHealthScope.Solution, snapshot.Build.Scope);
        Assert.Equal("Tests: impacted 3", snapshot.Tests.LineText);
        Assert.Equal(IdeHealthScope.Solution, snapshot.Tests.Scope);
    }

    [Fact]
    public void Compose_returns_project_segments_with_project_scope()
    {
        var decision = new IdeHealthScopeDecision(IdeHealthScope.Project, "src/App/App.csproj");

        var snapshot = IdeHealthBuildTestsUnit.Default.Compose(
            decision,
            buildState: new BuildStateSnapshot(false, 0, true),
            testSummary: "5/5 passed, 0 failed",
            impactedTestsBadge: 0);

        Assert.Equal("Build[src/App/App.csproj]: idle · last OK (exit 0)", snapshot.Build.LineText);
        Assert.Equal(IdeHealthScope.Project, snapshot.Build.Scope);
        Assert.Equal("src/App/App.csproj", snapshot.Build.ProjectPath);

        Assert.Contains("Tests[src/App/App.csproj]: 5/5 passed, 0 failed", snapshot.Tests.LineText, StringComparison.Ordinal);
        Assert.Equal(IdeHealthScope.Project, snapshot.Tests.Scope);
        Assert.Equal("src/App/App.csproj", snapshot.Tests.ProjectPath);
    }

    [Fact]
    public void IdeHealthBuildTestsUnit_Default_implements_ICockpitComputeUnit()
    {
        ICockpitComputeUnit unit = IdeHealthBuildTestsUnit.Default;
        Assert.NotNull(unit);
    }
}
