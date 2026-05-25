using System.Diagnostics;

namespace CascadeIDE.Features.Intercom.Admin;

/// <summary>Локальный supervisor <c>intercom-service</c> (ADR 0147 фаза 2).</summary>
public sealed class IntercomServerHostService : IDisposable
{
    private Process? _process;
    private string _lastStderr = "";

    public bool IsRunning => _process is { HasExited: false };

    public string DefaultBaseUrl { get; } = "http://127.0.0.1:5080";

    public (bool Ok, string Message) Start(string? baseUrl = null)
    {
        if (IsRunning)
            return (true, $"Уже запущен (PID {_process!.Id}).");

        var project = TryResolveServiceProjectPath();
        if (project is null)
            return (false, "Не найден IntercomService.csproj (host/intercom-service).");

        var url = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim().TrimEnd('/');
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{project}\" --urls {url}",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(project)!,
        };

        try
        {
            _process = Process.Start(psi);
            if (_process is null)
                return (false, "Process.Start вернул null.");

            _ = Task.Run(async () =>
            {
                try
                {
                    var err = await _process.StandardError.ReadToEndAsync().ConfigureAwait(false);
                    _lastStderr = err;
                }
                catch
                {
                    // ignore
                }
            });

            return (true, $"intercom-service запускается ({url}, PID {_process.Id}).");
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

            var msg = string.IsNullOrWhiteSpace(_lastStderr)
                ? "intercom-service остановлен."
                : $"intercom-service остановлен.\n{_lastStderr.Trim()}";
            _process.Dispose();
            _process = null;
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

        return $"intercom-service: PID {_process!.Id}, base {DefaultBaseUrl}.";
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

    public void Dispose() => Stop();
}
