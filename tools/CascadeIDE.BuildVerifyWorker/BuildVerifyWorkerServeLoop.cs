using System.Text.Json;
using DotNetBuildTest.Core;

namespace CascadeIDE.BuildVerifyWorker;

/// <summary>Long-lived verify worker: JSON-lines на stdin/stdout (ADR 0148 daemon, opt-in).</summary>
internal static class BuildVerifyWorkerServeLoop
{
    internal static async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var coordinator = new BuildTestJobCoordinator();
        await using var stdin = Console.OpenStandardInput();
        await using var stdout = Console.OpenStandardOutput();
        using var reader = new StreamReader(stdin);
        await using var writer = new StreamWriter(stdout) { AutoFlush = true, NewLine = "\n" };

        await WriteResponseAsync(
            writer,
            new Dictionary<string, object?>
            {
                ["id"] = "0",
                ["ok"] = true,
                ["ready"] = true,
                ["protocol"] = 1,
            },
            cancellationToken).ConfigureAwait(false);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
        {
            if (line.Length == 0)
                continue;

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(line);
            }
            catch (JsonException ex)
            {
                await WriteResponseAsync(
                    writer,
                    new Dictionary<string, object?> { ["id"] = "", ["ok"] = false, ["error"] = $"invalid_json: {ex.Message}" },
                    cancellationToken).ConfigureAwait(false);
                continue;
            }

            using (doc)
            {
                var root = doc.RootElement;
                var id = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
                var op = root.TryGetProperty("op", out var opEl) ? opEl.GetString() ?? "" : "";

                try
                {
                    var response = await DispatchAsync(coordinator, root, op, cancellationToken).ConfigureAwait(false);
                    response["id"] = id;
                    await WriteResponseAsync(writer, response, cancellationToken).ConfigureAwait(false);

                    if (string.Equals(op, "shutdown", StringComparison.OrdinalIgnoreCase))
                        return 0;
                }
                catch (Exception ex)
                {
                    await WriteResponseAsync(
                        writer,
                        new Dictionary<string, object?> { ["id"] = id, ["ok"] = false, ["error"] = ex.Message },
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return 0;
    }

    private static async Task<Dictionary<string, object?>> DispatchAsync(
        BuildTestJobCoordinator coordinator,
        JsonElement req,
        string op,
        CancellationToken cancellationToken)
    {
        switch (op.Trim().ToLowerInvariant())
        {
            case "ping":
                return new Dictionary<string, object?> { ["ok"] = true, ["pong"] = true };

            case "enqueue":
                return Enqueue(coordinator, req);

            case "get_status":
                return GetStatus(coordinator, req);

            case "wait":
                return await WaitAsync(coordinator, req, cancellationToken).ConfigureAwait(false);

            case "cancel":
                return Cancel(coordinator, req);

            case "shutdown":
                return new Dictionary<string, object?> { ["ok"] = true, ["shutting_down"] = true };

            default:
                return new Dictionary<string, object?> { ["ok"] = false, ["error"] = $"unknown_op:{op}" };
        }
    }

    private static Dictionary<string, object?> Enqueue(BuildTestJobCoordinator coordinator, JsonElement req)
    {
        var kindWire = req.TryGetProperty("kind", out var k) ? k.GetString() : null;
        var path = req.TryGetProperty("path", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<string, object?> { ["ok"] = false, ["error"] = "path_not_found" };

        var kind = string.Equals(kindWire, "test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(kindWire, "run_tests", StringComparison.OrdinalIgnoreCase)
                ? BuildTestJobKind.RunTests
                : BuildTestJobKind.BuildStructured;

        var includeRaw = !req.TryGetProperty("include_raw_output", out var raw) || raw.ValueKind != JsonValueKind.False;
        var timeout = req.TryGetProperty("timeout_seconds", out var t) && t.TryGetInt32(out var sec)
            ? sec
            : kind == BuildTestJobKind.RunTests
                ? BuildTestToolRequestParser.DefaultTestTimeoutSeconds
                : BuildTestToolRequestParser.DefaultBuildTimeoutSeconds;

        var filter = req.TryGetProperty("filter", out var f) ? f.GetString() : null;
        var dotnetOptions = !string.IsNullOrWhiteSpace(filter)
            ? DotnetExecutionOptions.Empty with { Filter = filter.Trim() }
            : DotnetExecutionOptions.Empty;

        var enqueued = coordinator.TryEnqueue(
            kind,
            Path.GetFullPath(path),
            includeRaw,
            timeout,
            dotnetOptions);

        if (!enqueued.Accepted)
        {
            return new Dictionary<string, object?>
            {
                ["ok"] = true,
                ["accepted"] = false,
                ["retry_after_seconds"] = enqueued.RetryAfterSeconds,
            };
        }

        return new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["accepted"] = true,
            ["job_id"] = enqueued.JobId,
        };
    }

    private static Dictionary<string, object?> GetStatus(BuildTestJobCoordinator coordinator, JsonElement req)
    {
        var jobId = req.TryGetProperty("job_id", out var j) ? j.GetString() : null;
        if (string.IsNullOrWhiteSpace(jobId))
            return new Dictionary<string, object?> { ["ok"] = false, ["error"] = "job_id_required" };

        var status = coordinator.GetJobStatus(jobId);
        if (status is null)
            return new Dictionary<string, object?> { ["ok"] = false, ["error"] = "job_not_found" };

        return new Dictionary<string, object?> { ["ok"] = true, ["status"] = status };
    }

    private static async Task<Dictionary<string, object?>> WaitAsync(
        BuildTestJobCoordinator coordinator,
        JsonElement req,
        CancellationToken cancellationToken)
    {
        var jobId = req.TryGetProperty("job_id", out var j) ? j.GetString() : null;
        if (string.IsNullOrWhiteSpace(jobId))
            return new Dictionary<string, object?> { ["ok"] = false, ["error"] = "job_id_required" };

        var result = await coordinator.WaitForCompletionAsync(jobId, cancellationToken).ConfigureAwait(false);
        return new Dictionary<string, object?>
        {
            ["ok"] = true,
            ["result_json"] = result ?? BuildTestJson.Serialize(new { success = false, message = "cancelled_or_missing" }),
        };
    }

    private static Dictionary<string, object?> Cancel(BuildTestJobCoordinator coordinator, JsonElement req)
    {
        var jobId = req.TryGetProperty("job_id", out var j) ? j.GetString() : null;
        if (string.IsNullOrWhiteSpace(jobId))
            return new Dictionary<string, object?> { ["ok"] = false, ["error"] = "job_id_required" };

        var outcome = coordinator.CancelJob(jobId);
        var json = JsonSerializer.Serialize(outcome);
        var cancelled = json.Contains("\"cancelled\":true", StringComparison.OrdinalIgnoreCase);
        return new Dictionary<string, object?> { ["ok"] = true, ["cancelled"] = cancelled, ["cancel"] = outcome };
    }

    private static async Task WriteResponseAsync(
        StreamWriter writer,
        Dictionary<string, object?> payload,
        CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(payload);
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
