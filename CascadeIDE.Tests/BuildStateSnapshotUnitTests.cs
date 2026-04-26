using CascadeIDE.Cockpit.ComputingUnits.IdeHealth;
using CascadeIDE.Cockpit.DataBus;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class BuildStateSnapshotUnitTests
{
    [Fact]
    public void Apply_running_sets_IsBuilding_preserves_last()
    {
        var prior = new BuildStateSnapshot(false, 0, true);
        var next = BuildStateSnapshotUnit.Apply(prior, new BuildStateChanged(true));
        Assert.True(next.IsBuilding);
        Assert.Equal(0, next.LastExitCode);
        Assert.True(next.LastBuildSucceeded);
    }

    [Fact]
    public void Apply_finish_updates_last_result()
    {
        var prior = BuildStateSnapshot.Empty;
        var after = BuildStateSnapshotUnit.Apply(prior, new BuildStateChanged(false, 3, false));
        Assert.False(after.IsBuilding);
        Assert.Equal(3, after.LastExitCode);
        Assert.False(after.LastBuildSucceeded);
    }

    [Fact]
    public void Apply_idle_without_metrics_keeps_prior_last()
    {
        var prior = new BuildStateSnapshot(false, 0, true);
        var after = BuildStateSnapshotUnit.Apply(prior, new BuildStateChanged(false));
        Assert.False(after.IsBuilding);
        Assert.Equal(0, after.LastExitCode);
        Assert.True(after.LastBuildSucceeded);
    }
}
