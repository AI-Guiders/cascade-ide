using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using DotNetBuildTest.Core;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>
/// Build/test через одноразовый <c>dotnet exec CascadeIDE.BuildVerifyWorker.dll</c> (ADR 0148 out-of-proc MLP).
/// </summary>
public sealed class OutOfProcessBuildVerifyWorkerBackend : IEnvironmentJobBackend
{
    private readonly string _workerDllPath;
    private readonly ConcurrentDictionary<string, WorkerJobRecord> _jobs = new();

    public OutOfProcessBuildVerifyWorkerBackend(string workerDllPath)
    {
        _workerDllPath = Path.GetFullPath(workerDllPath);
    }

    public string HostKind => "supervised-worker-process";

    public BuildTestEnqueueResult TryEnqueue(
        BuildTestJobKind kind,
        string path,
        bool includeRawOutput,
        int timeoutSeconds,
        DotnetExecutionOptions dotnetOptions)
    {
        _ = includeRawOutput;

        if (!File.Exists(_workerDllPath))
            return new BuildTestEnqueueResult(false, null, 5);

        var jobId = Guid.NewGuid().ToString("N");
        var record = new WorkerJobRecord(kind, path, timeoutSeconds, dotnetOptions);
        if (!_jobs.TryAdd(jobId, record))
            return new BuildTestEnqueueResult(false, null, 1);

        record.RunTask = Task.Run(() => RunWorkerProcessAsync(jobId, record));
        return new BuildTestEnqueueResult(true, jobId, 0);
    }

    public async Task<string?> WaitForCompletionAsync(string jobId, CancellationToken cancellationToken)
    {
        if (!_jobs.TryGetValue(jobId, out var record))
            return null;

        if (record.RunTask is not null)
            await record.RunTask.ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return record.ResultJson;
    }

    public object? GetJobStatus(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var record))
            return null;

        lock (record.Sync)
        {
            if (record.Cancelled)
                return new { status = "cancelled", cancelled = true, result = new { success = false } };

            if (!record.IsComplete)
                return new { status = "running" };

            var success = record.Success ?? false;
            return new
            {
                status = success ? "done" : "failed",
                result = new { success },
                success,
            };
        }
    }

    public object CancelJob(string jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var record))
            return new { cancelled = false, message = "not_found" };

        lock (record.Sync)
        {
            record.Cancelled = true;
            try
            {
                record.Process?.Kill(entireProcessTree: true);
            }
            catch
            {
                //
            }

            record.IsComplete = true;
            record.Success = false;
            record.ResultJson ??= JsonSerializer.Serialize(new { success = false, message = "cancelled" });
        }

        return new { cancelled = true };
    }

    private async Task RunWorkerProcessAsync(string jobId, WorkerJobRecord record)
    {
        var args = BuildWorkerArgs(record);
        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        if (record.DotnetOptions.SupplementalEnvironmentVariables is { Count: > 0 } env)
        {
            foreach (var pair in env)
                psi.Environment[pair.Key] = pair.Value;
        }

        using var proc = Process.Start(psi);
        if (proc is null)
        {
            Finish(record, false, JsonSerializer.Serialize(new { success = false, message = "process_start_failed" }));
            return;
        }

        lock (record.Sync)
            record.Process = proc;

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(30, record.TimeoutSeconds)));
        try
        {
            var stdoutTask = proc.StandardOutput.ReadToEndAsync(timeoutCts.Token);
            var stderrTask = proc.StandardError.ReadToEndAsync(timeoutCts.Token);
            await proc.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            var stdout = (await stdoutTask.ConfigureAwait(false)).Trim();
            var stderr = (await stderrTask.ConfigureAwait(false)).Trim();

            lock (record.Sync)
            {
                if (record.Cancelled)
                    return;
            }

            if (proc.ExitCode == 11)
            {
                Finish(record, false, JsonSerializer.Serialize(new { success = false, message = "busy" }));
                return;
            }

            if (string.IsNullOrWhiteSpace(stdout))
            {
                Finish(
                    record,
                    false,
                    JsonSerializer.Serialize(new { success = false, message = "empty_stdout", stderr }));
                return;
            }

            var success = proc.ExitCode == 0 && JsonTryGetBool(stdout, "success", out var ok) && ok;
            Finish(record, success, stdout);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!proc.HasExited)
                    proc.Kill(entireProcessTree: true);
            }
            catch
            {
                //
            }

            Finish(record, false, JsonSerializer.Serialize(new { success = false, message = "timed_out" }));
        }
        catch (Exception ex)
        {
            Finish(record, false, JsonSerializer.Serialize(new { success = false, message = ex.Message }));
        }
        finally
        {
            _jobs.TryRemove(jobId, out _);
        }
    }

    private string BuildWorkerArgs(WorkerJobRecord record)
    {
        var mode = record.Kind == BuildTestJobKind.RunTests ? "test" : "build";
        var quotedPath = QuoteArg(Path.GetFullPath(record.Path));
        var args = $"exec \"{_workerDllPath}\" {mode} {quotedPath}";

        if (record.Kind == BuildTestJobKind.RunTests
            && !string.IsNullOrWhiteSpace(record.DotnetOptions.Filter))
        {
            args += $" --filter {QuoteArg(record.DotnetOptions.Filter.Trim())}";
        }

        return args;
    }

    private static void Finish(WorkerJobRecord record, bool success, string resultJson)
    {
        lock (record.Sync)
        {
            record.IsComplete = true;
            record.Success = success;
            record.ResultJson = resultJson;
        }
    }

    private static bool JsonTryGetBool(string json, string prop, out bool value)
    {
        value = false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(prop, out var el)
                && (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False))
            {
                value = el.GetBoolean();
                return true;
            }
        }
        catch (JsonException)
        {
            //
        }

        return false;
    }

    private static string QuoteArg(string arg) =>
        arg.Contains(' ', StringComparison.Ordinal) || arg.Contains('"', StringComparison.Ordinal)
            ? $"\"{arg.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : arg;

    private sealed class WorkerJobRecord
    {
        public WorkerJobRecord(
            BuildTestJobKind kind,
            string path,
            int timeoutSeconds,
            DotnetExecutionOptions dotnetOptions)
        {
            Kind = kind;
            Path = path;
            TimeoutSeconds = timeoutSeconds;
            DotnetOptions = dotnetOptions;
        }

        public object Sync { get; } = new();
        public BuildTestJobKind Kind { get; }
        public string Path { get; }
        public int TimeoutSeconds { get; }
        public DotnetExecutionOptions DotnetOptions { get; }
        public Process? Process { get; set; }
        public Task? RunTask { get; set; }
        public bool IsComplete { get; set; }
        public bool Cancelled { get; set; }
        public bool? Success { get; set; }
        public string? ResultJson { get; set; }
    }
}
