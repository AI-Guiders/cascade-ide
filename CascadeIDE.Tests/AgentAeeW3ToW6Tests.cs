using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class AgentAeeW3ToW6Tests
{
    [Fact]
    public void EpochTracker_MarksUiStaleOnWrite()
    {
        var tracker = new AgentVerifyEpochTracker(new InMemoryDataBus());
        tracker.Begin("run1", "snap1", @"C:\ws\s.sln");
        tracker.WatchPath(@"C:\ws\Foo.cs");

        tracker.NotifyWrite(@"C:\ws\Foo.cs");

        Assert.True(tracker.IsUiStale);
        Assert.True(tracker.IsPathUiStale(@"C:\ws\Foo.cs"));
    }

    [Fact]
    public void DevServiceContractValidator_AcceptsEphemeralSubstrate()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cide-dev-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var bundle = AgentSandboxSubstrate.Allocate(dir);
            var lease = new AgentSandboxLease("r", AgentSandboxProfile.AgentEphemeral, dir, dir, bundle);
            var result = AgentDevServiceContractValidator.ValidateForTestScoped(
                new AgentDevServiceContractSettings(),
                AgentSandboxProfile.AgentEphemeral,
                lease);

            Assert.True(result.Ok);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void DevServiceContractValidator_FailsWithoutSubstrateWhenGated()
    {
        var lease = new AgentSandboxLease("r", AgentSandboxProfile.AgentEphemeral, "x", "x", Substrate: null);
        var result = AgentDevServiceContractValidator.ValidateForTestScoped(
            new AgentDevServiceContractSettings { GateTestScopedOnViolation = true },
            AgentSandboxProfile.AgentEphemeral,
            lease);

        Assert.False(result.Ok);
    }

    [Fact]
    public void SettingsToml_DeserializesDevServicesAndTestScopedFilter()
    {
        const string toml = """
            [agent.environment.dev_services]
            require_config_override = false
            gate_test_scoped_on_violation = false

            [agent.environment.ladder]
            test_scoped_touched_tests_only = false
            diagnose_files_enabled = true
            """;
        var s = CascadeIDE.Services.CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(toml)!;
        Assert.False(s.Agent.Environment.DevServices.RequireConfigOverride);
        Assert.False(s.Agent.Environment.DevServices.GateTestScopedOnViolation);
        Assert.False(s.Agent.Environment.Ladder.TestScopedTouchedTestsOnly);
        Assert.True(s.Agent.Environment.Ladder.DiagnoseFilesEnabled);
    }

    [Fact]
    public void AgentOrchestrator_SkipsWithoutWrites()
    {
        var svc = new AgentEnvironmentService(new InMemoryDataBus(), new AgentEnvironmentSettings());
        var orch = svc.Orchestrator;
        var result = orch.TryVerifyAfterStep(writesOccurred: false);
        Assert.False(result.Accepted);
    }

    [Fact]
    public void IdleUserTracker_RecordsSliceOnFocusReturn()
    {
        var tracker = new AgentIdleUserTracker();
        tracker.NotifyCideFocus(false);
        Thread.Sleep(60);
        tracker.NotifyCideFocus(true);
        var slices = tracker.DrainSlices();
        Assert.Contains(slices, s => s.Phase == AgentRunPhaseKind.IdleUser);
    }
}
