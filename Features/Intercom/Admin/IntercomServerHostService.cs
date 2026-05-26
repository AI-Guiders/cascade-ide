using System.Diagnostics;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Intercom.Admin;

/// <summary>Локальный supervisor <c>intercom-service</c> (ADR 0147 фаза 2).</summary>
public sealed class IntercomServerHostService : IDisposable
{
    private Process? _process;
    private string _lastStderr = "";
    private string _lastStdout = "";
    private string _lastLaunchSource = "";

    public bool IsRunning => _process is { HasExited: false };

    public string DefaultBaseUrl => IntercomTransportSettings.DefaultBaseUrl;

    public string LastLaunchSource => _lastLaunchSource;

    public (bool Ok, string Message) Start(string? baseUrl = null, string? configuredExecutablePath = null)
    {
        if (IsRunning)
            return (true, $"Уже запущен (PID {_process!.Id}, {_lastLaunchSource}).");

        var url = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim().TrimEnd('/');
        var launch = TryResolveLaunch(configuredExecutablePath);
        if (launch is null)
            return (false, "Не найден IntercomService (укажи [intercom.transport].local_server_path или выполни scripts/intercom/publish-intercom-service.ps1).");

        var psi = new ProcessStartInfo
        {
            FileName = launch.FileName,
            Arguments = string.IsNullOrWhiteSpace(launch.Arguments)
                ? $"--urls {url}"
                : $"{launch.Arguments} --urls {url}",
            UseShellExecute = false,
            // Child must not inherit IDE stdout: in --mcp-stdio it carries MCP JSON-RPC.
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = launch.WorkingDirectory,
        };
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        try
        {
            _process = Process.Start(psi);
            if (_process is null)
                return (false, "Process.Start вернул null.");

            _lastLaunchSource = launch.Source;
            _ = Task.Run(async () =>
            {
                try
                {
                    _lastStdout = await _process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            });
            _ = Task.Run(async () =>
            {
                try
                {
                    _lastStderr = await _process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                }
                catch
                {
                    // ignore
                }
            });

            return (true, $"intercom-service ({launch.Source}, {url}, PID {_process.Id}).");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public (bool Ok, string Message) Stop()
    {
        if (_process is null)
            return (true, "Сервер не запущен из CIDE.");

        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(5000);
            }

            var tail = FormatProcessLogTail(_lastStdout, _lastStderr);
            var msg = string.IsNullOrWhiteSpace(tail)
                ? "intercom-service остановлен."
                : $"intercom-service остановлен.\n{tail}";
            _process.Dispose();
            _process = null;
            _lastLaunchSource = "";
            _lastStdout = "";
            _lastStderr = "";
            return (true, msg);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public string DescribeStatus()
    {
        if (!IsRunning)
            return "intercom-service: не запущен из CIDE.";

        return $"intercom-service: PID {_process!.Id}, {_lastLaunchSource}, base {DefaultBaseUrl}.";
    }

    public static IntercomServerLaunchPlan? TryResolveLaunch(string? configuredExecutablePath)
    {
        var effective = string.IsNullOrWhiteSpace(configuredExecutablePath)
            ? IntercomTransportSettings.DefaultLocalServerRelativePath
            : configuredExecutablePath.Trim();

        if (TryNormalizeExistingFile(effective, out var configured))
            return configured;

        foreach (var candidate in EnumerateFallbackExecutableCandidates())
        {
            if (TryNormalizeExistingFile(candidate, out var plan))
                return plan;
        }

        var project = TryResolveServiceProjectPath();
        if (project is null)
            return null;

        return new IntercomServerLaunchPlan(
            FileName: "dotnet",
            Arguments: $"run --project \"{project}\"",
            WorkingDirectory: Path.GetDirectoryName(project)!,
            Source: "dotnet run (dev)");
    }

    public static IEnumerable<string> EnumerateFallbackExecutableCandidates()
    {
        foreach (var root in EnumerateSearchRoots())
        {
            yield return Path.Combine(root, "artifacts", "intercom-service", "IntercomService.exe");
            yield return Path.Combine(root, "host", "intercom-service", "publish", "IntercomService.exe");
            yield return Path.Combine(
                root,
                "host",
                "intercom-service",
                "src",
                "IntercomService",
                "bin",
                "Release",
                "net10.0",
                "win-x64",
                "publish",
                "IntercomService.exe");
            yield return Path.Combine(
                root,
                "host",
                "intercom-service",
                "src",
                "IntercomService",
                "bin",
                "Debug",
                "net10.0",
                "win-x64",
                "publish",
                "IntercomService.exe");
        }
    }

    private static bool TryNormalizeExistingFile(string? path, out IntercomServerLaunchPlan plan)
    {
        plan = default!;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        foreach (var full in ExpandPathCandidates(path.Trim().Trim('"')))
        {
            if (!File.Exists(full))
                continue;

            plan = new IntercomServerLaunchPlan(
                FileName: full,
                Arguments: "",
                WorkingDirectory: Path.GetDirectoryName(full) ?? AppContext.BaseDirectory,
                Source: "settings.local_server_path");
            return true;
        }

        return false;
    }

    private static IEnumerable<string> ExpandPathCandidates(string path)
    {
        if (Path.IsPathRooted(path))
        {
            yield return Path.GetFullPath(path);
            yield break;
        }

        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
        foreach (var root in EnumerateSearchRoots())
            yield return Path.GetFullPath(Path.Combine(root, path));
    }

    public static string? TryResolveServiceProjectPath()
    {
        foreach (var root in EnumerateSearchRoots())
        {
            var candidate = Path.Combine(
                root,
                "host",
                "intercom-service",
                "src",
                "IntercomService",
                "IntercomService.csproj");
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            for (var i = 0; i < 12 && dir is not null; i++, dir = dir.Parent)
            {
                var full = dir.FullName;
                if (seen.Add(full))
                    yield return full;
            }
        }
    }

    private static string FormatProcessLogTail(string stdout, string stderr)
    {
        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(stdout))
            parts.Add(stdout.Trim());
        if (!string.IsNullOrWhiteSpace(stderr))
            parts.Add(stderr.Trim());
        return string.Join(Environment.NewLine, parts);
    }

    public void Dispose() => Stop();
}

public sealed record IntercomServerLaunchPlan(
    string FileName,
    string Arguments,
    string WorkingDirectory,
    string Source);
