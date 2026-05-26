using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentVerifyEpochTrackerTests
{
    [Fact]
    public void NotifyWriteDuringActiveRun_SetsFlagAndStaleEvent()
    {
        var bus = new InMemoryDataBus();
        var stale = new List<AgentVerifyEpochStale>();
        bus.Subscribe<AgentVerifyEpochStale>(e => stale.Add(e));

        var t = new AgentVerifyEpochTracker(bus);
        var sln = Path.Combine(Path.GetTempPath(), "cide-verify-epoch-test.sln");
        File.WriteAllText(sln, "# stub");

        t.Begin("run1", "snap1", sln);
        Assert.False(t.WritesInvalidatedVerifyEpoch);

        t.NotifyWrite(Path.Combine(Path.GetTempPath(), "SomeFile.cs"));
        Assert.True(t.WritesInvalidatedVerifyEpoch);
        Assert.Single(stale);
        Assert.Equal("write_in_epoch", stale[0].Reason);

        t.End();
        Assert.False(t.WritesInvalidatedVerifyEpoch);

        try { File.Delete(sln); }
        catch { /* best-effort */ }
    }

    [Fact]
    public void ForBundle_HasExpectedKeys()
    {
        var root = Path.Combine(Path.GetTempPath(), "substrate-bundle-" + Guid.NewGuid().ToString("N"));
        try
        {
            var bundle = AgentSandboxSubstrate.Allocate(root);
            var env = AgentSandboxProcessEnvironmentKeys.ForBundle(bundle);
            Assert.Equal(bundle.DatabasePath, env[AgentSandboxProcessEnvironmentKeys.WitDbPath]);
            Assert.Equal(bundle.DevPort.ToString(), env[AgentSandboxProcessEnvironmentKeys.DevPort]);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); }
            catch { /* best-effort */ }
        }
    }
}
