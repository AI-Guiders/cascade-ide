using CascadeIDE.Cockpit.ComputingUnits;
using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthDebugSegmentUnitTests
{
    [Fact]
    public void Compose_returns_solution_debug_segment_for_solution_scope()
    {
        var decision = new IdeHealthScopeDecision(IdeHealthScope.Solution, ProjectPath: null);
        var snapshot = DebugSessionSnapshot.Empty with { HasActiveSession = true, IsExecutionStopped = false };

        var segment = IdeHealthDebugSegmentUnit.Default.Compose(decision, snapshot);

        Assert.Equal("Debug: running…", segment.LineText);
        Assert.Equal("DBG · run", segment.CockpitShort);
        Assert.Equal(IdeHealthScope.Solution, segment.Scope);
        Assert.Null(segment.ProjectPath);
    }

    [Fact]
    public void Compose_returns_project_debug_segment_for_project_scope()
    {
        var decision = new IdeHealthScopeDecision(IdeHealthScope.Project, "src/App/App.csproj");
        var snapshot = DebugSessionSnapshot.Empty with
        {
            HasActiveSession = true,
            IsExecutionStopped = true,
            StackFrames = new (string Name, string? File, int Line)[] { ("Main", "Program.cs", 10) },
            VariableRootScopes = new[]
            {
                new DebugVariableRootScope("Locals", new[] { new DebugVariableRow("x", "1", "int") })
            }
        };

        var segment = IdeHealthDebugSegmentUnit.Default.Compose(decision, snapshot);

        Assert.Contains("Debug[src/App/App.csproj]: paused · frames 1, vars 1", segment.LineText, StringComparison.Ordinal);
        Assert.Equal(IdeHealthScope.Project, segment.Scope);
        Assert.Equal("src/App/App.csproj", segment.ProjectPath);
    }

    [Fact]
    public void IdeHealthDebugSegmentUnit_Default_implements_ICockpitComputeUnit()
    {
        ICockpitComputeUnit unit = IdeHealthDebugSegmentUnit.Default;
        Assert.NotNull(unit);
    }
}
