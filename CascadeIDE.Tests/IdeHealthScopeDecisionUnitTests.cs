using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthScopeDecisionUnitTests
{
    [Fact]
    public void Decide_returns_solution_scope_without_startup_project()
    {
        var decision = IdeHealthScopeDecisionUnit.Default.Decide(
            startupProjectPath: null,
            isBuilding: true,
            lastTestSummary: "5/5 passed");

        Assert.Equal(IdeHealthScope.Solution, decision.Scope);
        Assert.Null(decision.ProjectPath);
    }

    [Fact]
    public void Decide_returns_solution_scope_without_build_or_tests_signal()
    {
        var decision = IdeHealthScopeDecisionUnit.Default.Decide(
            startupProjectPath: "src/App/App.csproj",
            isBuilding: false,
            lastTestSummary: "");

        Assert.Equal(IdeHealthScope.Solution, decision.Scope);
        Assert.Null(decision.ProjectPath);
    }

    [Fact]
    public void Decide_returns_project_scope_when_startup_project_and_signal_exist()
    {
        var decision = IdeHealthScopeDecisionUnit.Default.Decide(
            startupProjectPath: "src/App/App.csproj",
            isBuilding: false,
            lastTestSummary: "12/12 passed");

        Assert.Equal(IdeHealthScope.Project, decision.Scope);
        Assert.Equal("src/App/App.csproj", decision.ProjectPath);
    }
}
