using System.Text.Json;
using DotNetBuildTest.Core;

namespace CascadeIDE.BuildVerifyWorker;

/// <summary>
/// Одноразовый воркер verify: <c>BuildTestJobCoordinator</c> вне процесса CascadeIDE (ADR 0148 / out-of-proc scaffold).
/// MSBuild/VSTest выполняются через тот же пайплайн, что и в IDE — <see cref="DotnetProcessRunner"/>.
/// </summary>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine(
                "Usage: CascadeIDE.BuildVerifyWorker build <path-to-sln>|csproj | test <path-to-sln>|csproj [--filter <xunit filter>]");
            return 2;
        }

        var mode = args[0].Trim().ToLowerInvariant();
        var path = Path.GetFullPath(args[1].Trim());
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"File not found: {path}");
            return 2;
        }

        string? filter = null;
        for (var i = 2; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "--filter", StringComparison.OrdinalIgnoreCase))
                filter = args[i + 1];
        }

        var coordinator = new BuildTestJobCoordinator();

        var dotnetOptions = mode == "test" && !string.IsNullOrWhiteSpace(filter)
            ? DotnetExecutionOptions.Empty with { Filter = filter.Trim() }
            : DotnetExecutionOptions.Empty;

        var kind = mode switch
        {
            "build" => BuildTestJobKind.BuildStructured,
            "test" => BuildTestJobKind.RunTests,
            _ => (BuildTestJobKind?)null,
        };

        if (kind is null)
        {
            Console.Error.WriteLine("Mode must be \"build\" or \"test\".");
            return 2;
        }

        var timeout = kind == BuildTestJobKind.RunTests
            ? BuildTestToolRequestParser.DefaultTestTimeoutSeconds
            : BuildTestToolRequestParser.DefaultBuildTimeoutSeconds;

        var enqueued = coordinator.TryEnqueue(kind.Value, path, includeRawOutput: true, timeout, dotnetOptions);
        if (!enqueued.Accepted)
        {
            Console.WriteLine(
                BuildTestJson.Serialize(new
                {
                    success = false,
                    message = "busy",
                    retry_after_seconds = enqueued.RetryAfterSeconds,
                }));
            return 11;
        }

        var result = await coordinator.WaitForCompletionAsync(enqueued.JobId!, CancellationToken.None).ConfigureAwait(false)
            ?? BuildTestJson.Serialize(new { success = false, message = "cancelled_or_missing" });

        Console.WriteLine(result);
        return JsonTryGetBool(result, "success", out var ok) && ok ? 0 : 1;
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
}
