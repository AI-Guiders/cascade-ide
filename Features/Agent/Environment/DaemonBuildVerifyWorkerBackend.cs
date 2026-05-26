using System.Collections.Concurrent;
using System.Text.Json;
using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>
/// Build/test через long-lived <c>BuildVerifyWorker serve</c> (ADR 0148 opt-in daemon).
/// </summary>
public sealed class DaemonBuildVerifyWorkerBackend : IEnvironmentJobBackend, IAsyncDisposable
{
    private readonly BuildVerifyWorkerDaemonClient _client;
    private readonly ConcurrentDictionary<string, DaemonJobRecord> _jobs = new();

    public DaemonBuildVerifyWorkerBackend(string workerDllPath)
    {
        _client = new BuildVerifyWorkerDaemonClient(workerDllPath);
    }

    public string HostKind => BuildTestHostFactory.WorkerDaemonHostKind;

    public BuildTestEnqueueResult TryEnqueue(
        BuildTestJobKind kind,
        string path,
        bool includeRawOutput,
        int timeoutSeconds,
        DotnetExecutionOptions dotnetOptions)
    {
        try
        {
            var resp = _client
                .SendAsync(
                    BuildEnqueuePayload(kind, path, includeRawOutput, timeoutSeconds, dotnetOptions),
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (!resp.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                return new BuildTestEnqueueResult(false, null, 5);

            if (resp.TryGetProperty("accepted", out var accepted) && accepted.ValueKind == JsonValueKind.False)
            {
                var retry = resp.TryGetProperty("retry_after_seconds", out var r) && r.TryGetInt32(out var sec)
                    ? sec
                    : 5;
                return new BuildTestEnqueueResult(false, null, retry);
            }

            var jobId = resp.TryGetProperty("job_id", out var j) ? j.GetString() : null;
            if (string.IsNullOrWhiteSpace(jobId))
                return new BuildTestEnqueueResult(false, null, 5);

            _jobs[jobId] = new DaemonJobRecord();
            return new BuildTestEnqueueResult(true, jobId, 0);
        }
        catch
        {
            return new BuildTestEnqueueResult(false, null, 5);
        }
    }

    public async Task<string?> WaitForCompletionAsync(string jobId, CancellationToken cancellationToken)
    {
        if (!_jobs.ContainsKey(jobId))
            return null;

        var resp = await _client
            .SendAsync(
                new Dictionary<string, object?> { ["op"] = "wait", ["job_id"] = jobId },
                cancellationToken)
            .ConfigureAwait(false);

        if (!resp.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
            return null;

        var json = resp.TryGetProperty("result_json", out var r) ? r.GetString() : null;
        if (_jobs.TryGetValue(jobId, out var record))
        {
            record.IsComplete = true;
            record.ResultJson = json;
            record.Success = TryReadSuccess(json);
        }

        _jobs.TryRemove(jobId, out _);
        return json;
    }

    public object? GetJobStatus(string jobId)
    {
        if (!_jobs.ContainsKey(jobId))
            return null;

        try
        {
            var resp = _client
                .SendAsync(
                    new Dictionary<string, object?> { ["op"] = "get_status", ["job_id"] = jobId },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            if (!resp.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                return null;

            if (!resp.TryGetProperty("status", out var status))
                return null;

            return JsonSerializer.Deserialize<object>(status.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    public object CancelJob(string jobId)
    {
        try
        {
            var resp = _client
                .SendAsync(
                    new Dictionary<string, object?> { ["op"] = "cancel", ["job_id"] = jobId },
                    CancellationToken.None)
                .GetAwaiter()
                .GetResult();

            var cancelled = resp.TryGetProperty("cancelled", out var c)
                && c.ValueKind == JsonValueKind.True;

            if (_jobs.TryGetValue(jobId, out var record))
            {
                record.IsComplete = true;
                record.Success = false;
                record.ResultJson = JsonSerializer.Serialize(new { success = false, message = "cancelled" });
            }

            _jobs.TryRemove(jobId, out _);
            return new { cancelled };
        }
        catch
        {
            return new { cancelled = false };
        }
    }

    public async ValueTask DisposeAsync() => await _client.DisposeAsync().ConfigureAwait(false);

    private static Dictionary<string, object?> BuildEnqueuePayload(
        BuildTestJobKind kind,
        string path,
        bool includeRawOutput,
        int timeoutSeconds,
        DotnetExecutionOptions dotnetOptions)
    {
        var payload = new Dictionary<string, object?>
        {
            ["op"] = "enqueue",
            ["kind"] = kind == BuildTestJobKind.RunTests ? "test" : "build",
            ["path"] = Path.GetFullPath(path),
            ["include_raw_output"] = includeRawOutput,
            ["timeout_seconds"] = timeoutSeconds,
        };

        if (!string.IsNullOrWhiteSpace(dotnetOptions.Filter))
            payload["filter"] = dotnetOptions.Filter.Trim();

        return payload;
    }

    private static bool TryReadSuccess(string? resultJson)
    {
        if (string.IsNullOrWhiteSpace(resultJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(resultJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("result", out var result)
                && result.ValueKind == JsonValueKind.Object
                && result.TryGetProperty("success", out var nested))
            {
                return nested.ValueKind == JsonValueKind.True;
            }

            if (root.TryGetProperty("success", out var direct))
                return direct.ValueKind == JsonValueKind.True;
        }
        catch (JsonException)
        {
            //
        }

        return false;
    }

    private sealed class DaemonJobRecord
    {
        public bool IsComplete { get; set; }
        public bool? Success { get; set; }
        public string? ResultJson { get; set; }
    }
}
