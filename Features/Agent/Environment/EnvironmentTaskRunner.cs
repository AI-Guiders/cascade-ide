using System.Text.Json;
using CascadeIDE.Cockpit.DataBus;
using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Очередь environment tasks с DataBus-событиями (ADR 0148 W1).</summary>
public sealed class EnvironmentTaskRunner
{
    private readonly IDataBus _dataBus;
    private readonly BuildTestJobCoordinator _coordinator;

    public EnvironmentTaskRunner(IDataBus dataBus, BuildTestJobCoordinator coordinator)
    {
        _dataBus = dataBus;
        _coordinator = coordinator;
    }

    public BuildTestJobCoordinator Coordinator => _coordinator;

    public async Task<EnvironmentTaskOutcome> RunBuildAsync(
        string runId,
        string solutionPath,
        bool waitForCompletion,
        CancellationToken cancellationToken = default)
    {
        return await RunCoreJobAsync(
            runId,
            "msbuild.compile",
            BuildTestJobKind.BuildStructured,
            solutionPath,
            includeRawOutput: true,
            BuildTestToolRequestParser.DefaultBuildTimeoutSeconds,
            DotnetExecutionOptions.Empty,
            waitForCompletion,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<EnvironmentTaskOutcome> RunTestsAsync(
        string runId,
        string solutionPath,
        string? filterExpression,
        bool waitForCompletion,
        CancellationToken cancellationToken = default)
    {
        var options = string.IsNullOrWhiteSpace(filterExpression)
            ? DotnetExecutionOptions.Empty
            : DotnetExecutionOptions.Empty with { Filter = filterExpression.Trim() };

        return await RunCoreJobAsync(
            runId,
            "dotnet.test",
            BuildTestJobKind.RunTests,
            solutionPath,
            includeRawOutput: true,
            BuildTestToolRequestParser.DefaultTestTimeoutSeconds,
            options,
            waitForCompletion,
            cancellationToken).ConfigureAwait(false);
    }

    public bool TryCancelCoreJob(string coreJobId) =>
        _coordinator.CancelJob(coreJobId) is { } o
        && JsonSerializer.Serialize(o).Contains("\"cancelled\":true", StringComparison.Ordinal);

    private async Task<EnvironmentTaskOutcome> RunCoreJobAsync(
        string runId,
        string kind,
        BuildTestJobKind coreKind,
        string solutionPath,
        bool includeRawOutput,
        int timeoutSeconds,
        DotnetExecutionOptions dotnetOptions,
        bool waitForCompletion,
        CancellationToken cancellationToken)
    {
        var taskId = Guid.NewGuid().ToString("N");
        PublishTaskChanged(taskId, runId, kind, AgentEnvironmentTaskState.Queued, "queued");

        var enqueued = _coordinator.TryEnqueue(
            coreKind,
            solutionPath,
            includeRawOutput,
            timeoutSeconds,
            dotnetOptions);

        if (!enqueued.Accepted)
        {
            PublishTaskChanged(taskId, runId, kind, AgentEnvironmentTaskState.Failed, "worker busy");
            return new EnvironmentTaskOutcome(taskId, null, false, "busy", null);
        }

        var coreJobId = enqueued.JobId!;
        _ = WatchJobAsync(taskId, runId, kind, coreJobId, cancellationToken);

        if (!waitForCompletion)
            return new EnvironmentTaskOutcome(taskId, coreJobId, true, "queued", null);

        var resultJson = await _coordinator.WaitForCompletionAsync(coreJobId, cancellationToken).ConfigureAwait(false);
        var success = TryReadSuccess(resultJson);
        return new EnvironmentTaskOutcome(taskId, coreJobId, success, success ? "completed" : "failed", resultJson);
    }

    private async Task WatchJobAsync(
        string taskId,
        string runId,
        string kind,
        string coreJobId,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        PublishTaskChanged(taskId, runId, kind, AgentEnvironmentTaskState.Running, "running");

        while (!cancellationToken.IsCancellationRequested)
        {
            var statusObj = _coordinator.GetJobStatus(coreJobId);
            if (statusObj is null)
            {
                PublishDied(taskId, runId, kind, null, "job not found");
                return;
            }

            var json = JsonSerializer.Serialize(statusObj);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var state = root.TryGetProperty("status", out var st) ? st.GetString() : null;

            if (state is "done" or "failed" or "cancelled" or "timed_out")
            {
                var success = TryReadSuccessFromStatus(root);
                var durationMs = (int)(DateTimeOffset.UtcNow - started).TotalMilliseconds;
                var summary = state switch
                {
                    "cancelled" => "cancelled",
                    "timed_out" => "timed out",
                    _ => success ? "green" : "failed",
                };

                PublishTaskChanged(
                    taskId,
                    runId,
                    kind,
                    state == "cancelled"
                        ? AgentEnvironmentTaskState.Cancelled
                        : success
                            ? AgentEnvironmentTaskState.Completed
                            : AgentEnvironmentTaskState.Failed,
                    summary);

                _dataBus.Publish(new AgentEnvironmentTaskCompleted(taskId, runId, kind, summary, durationMs));
                return;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }

    private void PublishTaskChanged(
        string taskId,
        string runId,
        string kind,
        AgentEnvironmentTaskState state,
        string? message) =>
        _dataBus.Publish(new AgentEnvironmentTaskChanged(taskId, runId, kind, state, message));

    private void PublishDied(string taskId, string runId, string kind, int? exitCode, string? tail) =>
        _dataBus.Publish(new AgentEnvironmentTaskDied(taskId, runId, kind, exitCode, tail));

    private static bool TryReadSuccess(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            return TryReadSuccessFromStatus(doc.RootElement);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadSuccessFromStatus(JsonElement root)
    {
        if (root.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Object
            && result.TryGetProperty("success", out var nested))
        {
            return nested.ValueKind == JsonValueKind.True;
        }

        if (root.TryGetProperty("success", out var direct))
            return direct.ValueKind == JsonValueKind.True;

        return false;
    }
}

public sealed record EnvironmentTaskOutcome(
    string TaskId,
    string? CoreJobId,
    bool Success,
    string Status,
    string? ResultJson);
