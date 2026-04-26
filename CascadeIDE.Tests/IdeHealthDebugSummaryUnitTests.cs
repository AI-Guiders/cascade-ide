using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IdeHealthDebugSummaryUnitTests
{
    [Fact]
    public void Summarize_returns_idle_without_active_session()
    {
        var summary = IdeHealthDebugSummaryUnit.Default.Summarize(DebugSessionSnapshot.Empty);
        Assert.Equal("idle", summary);
    }

    [Fact]
    public void Summarize_returns_running_for_active_non_stopped_session()
    {
        var snapshot = DebugSessionSnapshot.Empty with { HasActiveSession = true, IsExecutionStopped = false };
        var summary = IdeHealthDebugSummaryUnit.Default.Summarize(snapshot);
        Assert.Equal("running…", summary);
    }

    [Fact]
    public void Summarize_returns_paused_with_frames_and_variables()
    {
        var snapshot = DebugSessionSnapshot.Empty with
        {
            HasActiveSession = true,
            IsExecutionStopped = true,
            StackFrames = new (string Name, string? File, int Line)[] { ("Main", "Program.cs", 10), ("Worker", "Worker.cs", 42) },
            VariableRootScopes = new[]
            {
                new DebugVariableRootScope("Locals", new[] { new DebugVariableRow("a", "1", "int"), new DebugVariableRow("b", "\"x\"", "string") }),
                new DebugVariableRootScope("Arguments", new[] { new DebugVariableRow("arg", "true", "bool") })
            }
        };

        var summary = IdeHealthDebugSummaryUnit.Default.Summarize(snapshot);
        Assert.Equal("paused · frames 2, vars 3", summary);
    }
}
