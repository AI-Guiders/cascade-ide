using System.Diagnostics;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

/// <summary>
/// Orion local test-drive scenarios (ADR 0148): stale/coalesce, substrate isolation, host death.
/// </summary>
[Trait("Category", "AgentEnvironment")]
public sealed class AgentEnvironmentOrionStressTests
{
    private static string? TryResolveCascadeSolutionPath()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "CascadeIDE.sln");
            if (File.Exists(candidate))
                return candidate;

            var parent = Directory.GetParent(dir);
            if (parent is null)
                break;
            dir = parent.FullName;
        }

        return null;
    }

    [Fact]
    public void Stress_Dedup_CoalescesBuildKeyWithin1500ms()
    {
        var dedup = new EnvironmentTaskDedup(1500);
        var key = "build|solution.sln";

        Assert.False(dedup.ShouldCoalesce(key));
        Assert.True(dedup.ShouldCoalesce(key));

        Thread.Sleep(1600);
        Assert.False(dedup.ShouldCoalesce(key));
    }

    [Fact]
    public async Task Stress_MicroWrites_EmitsEpochStaleDuringActiveVerify()
    {
        var bus = new InMemoryDataBus();
        var staleEvents = new List<AgentVerifyEpochStale>();
        bus.Subscribe<AgentVerifyEpochStale>(e => staleEvents.Add(e));

        var tracker = new AgentVerifyEpochTracker(bus);
        tracker.Begin("run-a", "snap-1", @"C:\repo\app.sln");

        for (var i = 0; i < 20; i++)
        {
            tracker.NotifyWrite(@"C:\repo\src\File" + i + ".cs");
            await Task.Delay(50);
        }

        Assert.True(staleEvents.Count >= 15);
        Assert.All(staleEvents, e => Assert.Equal("write_in_epoch", e.Reason));
    }

    [Fact]
    public async Task Stress_RapidVerify_SupersedesPredecessor_NotParallelActive()
    {
        var sln = TryResolveCascadeSolutionPath();
        if (sln is null)
            return;

        var bus = new InMemoryDataBus();
        var superseded = 0;
        bus.Subscribe<AgentVerifyEpochStale>(e =>
        {
            if (e.Reason == "superseded")
                Interlocked.Increment(ref superseded);
        });

        var settings = new AgentEnvironmentSettings { CoalesceWindowMs = 1500 };
        var svc = new AgentEnvironmentService(bus, settings);

        var sawActiveBeforeSecondStart = false;
        for (var i = 0; i < 15; i++)
        {
            var wasActive = svc.GetStatus().IsActive;
            var start = svc.StartVerify(sln, AgentVerifyPolicy.Minimal);
            Assert.True(start.Accepted);
            Assert.True(svc.GetStatus().IsActive);
            if (i > 0 && wasActive)
                sawActiveBeforeSecondStart = true;
            await Task.Delay(30);
        }

        Assert.True(superseded >= 1, $"expected at least one superseded stale, got {superseded}");
        Assert.True(sawActiveBeforeSecondStart || superseded >= 2, "rapid verify should overlap or supersede");

        svc.CancelActive();
        await Task.Delay(100);
        Assert.False(svc.GetStatus().IsActive);
    }

    [Fact]
    public async Task Stress_ParallelSubstrate_PortsAndDbDoNotLeak()
    {
        var root = Path.Combine(Path.GetTempPath(), "cide-orion-" + Guid.NewGuid().ToString("N"));
        var manager = new AgentSandboxManager(Path.Combine(root, "agent-runs"));

        try
        {
            var runs = await Task.WhenAll(Enumerable.Range(0, 3).Select(async i =>
            {
                var runId = $"parallel-{i}";
                var lease = manager.Prepare(runId, AgentSandboxProfile.AgentEphemeral);
                var bundle = lease.Substrate ?? throw new InvalidOperationException("substrate missing");
                var payload = $"owner-{runId}";
                await Task.Run(() =>
                {
                    AgentSandboxSubstrate.WriteHeavyMarker(bundle, payload);
                    Thread.Sleep(200);
                });
                return (runId, bundle, payload);
            }));

            var ports = runs.Select(r => r.bundle.DevPort).ToArray();
            Assert.Equal(ports.Length, ports.Distinct().Count());

            foreach (var (runId, bundle, payload) in runs)
            {
                var dbOwner = AgentSandboxSubstrate.ReadDatabaseOwner(bundle);
                Assert.Equal(payload, dbOwner);
                Assert.Contains(runId, File.ReadAllText(bundle.MarkerPath));
            }

            var secondWave = runs.Select(r => manager.RecreateSubstrateBeforeTests(
                new AgentSandboxLease(r.runId, AgentSandboxProfile.AgentEphemeral, Path.Combine(manager.RunsRoot, r.runId), null, r.bundle))).ToList();

            Assert.Equal(3, secondWave.Select(b => b.DevPort).Distinct().Count());
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // best-effort
            }
        }
    }

    [Fact]
    public async Task Stress_HostDeath_PublishesDiedQuickly()
    {
        var bus = new InMemoryDataBus();
        AgentEnvironmentTaskDied? died = null;
        var diedAtMs = -1L;
        var sw = Stopwatch.StartNew();
        bus.Subscribe<AgentEnvironmentTaskDied>(e =>
        {
            died = e;
            diedAtMs = sw.ElapsedMilliseconds;
        });

        var coordinator = new DotNetBuildTest.Core.BuildTestJobCoordinator();
        var runner = new EnvironmentTaskRunner(bus, coordinator)
        {
            TestJobStatusFactory = _ => null,
        };

        var sln = TryResolveCascadeSolutionPath()
            ?? throw new InvalidOperationException("CascadeIDE.sln not found for host-death stress test.");

        var outcome = await runner.RunBuildAsync("run-death", sln, waitForCompletion: false);
        Assert.True(
            outcome.CoreJobId is not null || outcome.Status is "queued",
            $"build task not enqueued: {outcome.Status}");

        for (var i = 0; i < 30 && died is null; i++)
            await Task.Delay(50);

        Assert.NotNull(died);
        Assert.True(diedAtMs < 2000, $"Died event too slow: {diedAtMs}ms");
        Assert.Equal("supervised-inproc", died!.HostKind);
    }

    [Fact]
    public async Task Stress_RapidVerifyAndMicroWrites_NoUnhandledException()
    {
        var sln = TryResolveCascadeSolutionPath();
        if (sln is null)
            return;

        var bus = new InMemoryDataBus();
        var svc = new AgentEnvironmentService(bus, new AgentEnvironmentSettings { CoalesceWindowMs = 1500 });
        var scratch = Path.Combine(Path.GetTempPath(), "cide-scratch-" + Guid.NewGuid().ToString("N") + ".cs");

        try
        {
            await File.WriteAllTextAsync(scratch, "// scratch");
            var writeTask = Task.Run(async () =>
            {
                for (var i = 0; i < 30; i++)
                {
                    await File.AppendAllTextAsync(scratch, "\n// " + i);
                    svc.EpochTracker.NotifyWrite(scratch);
                    await Task.Delay(500);
                }
            });

            var verifyTask = Task.Run(async () =>
            {
                for (var i = 0; i < 10; i++)
                {
                    svc.StartVerify(sln, AgentVerifyPolicy.Minimal);
                    await Task.Delay(500);
                }
            });

            await Task.WhenAll(writeTask, verifyTask);
        }
        finally
        {
            try
            {
                File.Delete(scratch);
            }
            catch
            {
                // best-effort
            }

            svc.CancelActive();
        }
    }
}
