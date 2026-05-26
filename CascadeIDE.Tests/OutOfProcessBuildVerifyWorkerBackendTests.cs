using System.Text.Json;
using CascadeIDE.Cockpit.DataBus;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Models;
using DotNetBuildTest.Core;
using Xunit;

namespace CascadeIDE.Tests;

[Trait("Category", "BuildVerifyWorker")]
public sealed class OutOfProcessBuildVerifyWorkerBackendTests
{
    private static string WorkerDllPath()
    {
        var inOutput = Path.Combine(AppContext.BaseDirectory, "CascadeIDE.BuildVerifyWorker.dll");
        if (File.Exists(inOutput))
            return inOutput;

        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CascadeIDE.sln")))
            {
                return Path.Combine(
                    dir.FullName,
                    "tools",
                    "CascadeIDE.BuildVerifyWorker",
                    "bin",
                    "Debug",
                    "net10.0",
                    "CascadeIDE.BuildVerifyWorker.dll");
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException("BuildVerifyWorker assembly not found.");
    }

    private static string SampleCsprojPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "samples", "DebugTarget", "DebugTarget.csproj");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("DebugTarget.csproj not found.");
    }

    [Fact]
    public async Task Backend_build_sample_csproj_completes_with_success()
    {
        var backend = new OutOfProcessBuildVerifyWorkerBackend(WorkerDllPath());
        var enqueued = backend.TryEnqueue(
            BuildTestJobKind.BuildStructured,
            SampleCsprojPath(),
            includeRawOutput: true,
            BuildTestToolRequestParser.DefaultBuildTimeoutSeconds,
            DotnetExecutionOptions.Empty);

        Assert.True(enqueued.Accepted);
        Assert.NotNull(enqueued.JobId);

        var json = await backend.WaitForCompletionAsync(enqueued.JobId!, CancellationToken.None);
        Assert.NotNull(json);
        Assert.Contains("\"success\":true", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildTestHostFactory_worker_mode_uses_worker_backend()
    {
        var host = BuildTestHostFactory.Create(
            new AgentEnvironmentSettings { BuildVerifyHost = BuildTestHostFactory.WorkerProcessHostKind },
            new BuildTestJobCoordinator());

        Assert.Equal(BuildTestHostFactory.WorkerProcessHostKind, host.HostKind);
        Assert.IsType<OutOfProcessBuildVerifyWorkerBackend>(host.JobBackend);
    }

    [Fact]
    public void BuildTestHostFactory_daemon_mode_uses_daemon_backend()
    {
        var host = BuildTestHostFactory.Create(
            new AgentEnvironmentSettings { BuildVerifyHost = BuildTestHostFactory.WorkerDaemonHostKind },
            new BuildTestJobCoordinator());

        Assert.Equal(BuildTestHostFactory.WorkerDaemonHostKind, host.HostKind);
        Assert.IsType<DaemonBuildVerifyWorkerBackend>(host.JobBackend);
    }

    [Fact]
    public async Task Daemon_backend_build_sample_csproj_completes_with_success()
    {
        await using var backend = new DaemonBuildVerifyWorkerBackend(WorkerDllPath());
        var enqueued = backend.TryEnqueue(
            BuildTestJobKind.BuildStructured,
            SampleCsprojPath(),
            includeRawOutput: true,
            BuildTestToolRequestParser.DefaultBuildTimeoutSeconds,
            DotnetExecutionOptions.Empty);

        Assert.True(enqueued.Accepted);
        Assert.NotNull(enqueued.JobId);

        var json = await backend.WaitForCompletionAsync(enqueued.JobId!, CancellationToken.None);
        Assert.NotNull(json);
        Assert.Contains("\"success\":true", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Daemon_client_ping_and_enqueue_wait()
    {
        await using var client = new BuildVerifyWorkerDaemonClient(WorkerDllPath());
        await client.EnsureStartedAsync();

        var ping = await client.SendAsync(new Dictionary<string, object?> { ["op"] = "ping" });
        Assert.True(ping.TryGetProperty("pong", out var pong) && pong.ValueKind == JsonValueKind.True);

        var enq = await client.SendAsync(new Dictionary<string, object?>
        {
            ["op"] = "enqueue",
            ["kind"] = "build",
            ["path"] = SampleCsprojPath(),
            ["include_raw_output"] = true,
            ["timeout_seconds"] = BuildTestToolRequestParser.DefaultBuildTimeoutSeconds,
        });

        Assert.True(enq.TryGetProperty("accepted", out var acc) && acc.ValueKind == JsonValueKind.True);
        var jobId = enq.GetProperty("job_id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(jobId));

        var wait = await client.SendAsync(new Dictionary<string, object?>
        {
            ["op"] = "wait",
            ["job_id"] = jobId,
        });

        Assert.True(wait.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True);
        var result = wait.GetProperty("result_json").GetString();
        Assert.Contains("\"success\":true", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EnvironmentTaskRunner_daemon_backend_reports_host_kind()
    {
        var bus = new InMemoryDataBus();
        await using var backend = new DaemonBuildVerifyWorkerBackend(WorkerDllPath());
        var runner = new EnvironmentTaskRunner(bus, backend);
        var csproj = SampleCsprojPath();

        var outcome = await runner.RunBuildAsync("run-daemon", csproj, waitForCompletion: true);

        Assert.True(outcome.Success, outcome.ResultJson ?? outcome.Status);
        Assert.Equal(BuildTestHostFactory.WorkerDaemonHostKind, backend.HostKind);
    }
}
