#nullable enable

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Thin redirected <c>git status -sb</c> (+ short log) for Glass Git MFD.
/// Full panel SSOT remains Avalonia <c>GitMfdPageView</c> / GitPanel.
/// </summary>
internal sealed class GlassRedirectedGit : IDisposable
{
    static readonly Regex AnsiCsi = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]|\x1B\].*?\x07|\x1B\].*?\x1B\\", RegexOptions.Compiled);

    Process? _process;
    CancellationTokenSource? _pump;

    public event Action<string>? TextReceived;
    public event Action<int>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public string DisplayTarget { get; private set; } = "status";

    public void Start(string workingDirectory)
    {
        if (IsRunning)
            return;

        var cwd = ResolveCwd(workingDirectory);
        DisplayTarget = "status -sb";

        // One shell line: status + recent commits (thin peel; not full GitPanel).
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = cwd,
        };
        startInfo.ArgumentList.Add("-C");
        startInfo.ArgumentList.Add(cwd);
        startInfo.ArgumentList.Add("status");
        startInfo.ArgumentList.Add("-sb");
        startInfo.Environment["TERM"] = "dumb";
        startInfo.Environment["GIT_PAGER"] = "cat";

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnExited;

        TextReceived?.Invoke($"┌ Glass redirected · git {DisplayTarget}\n");
        TextReceived?.Invoke($"│ cwd {cwd}\n\n");

        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            TextReceived?.Invoke($"┌ start fail · {ex.Message} ┐\n");
            Exited?.Invoke(-1);
            Dispose();
            return;
        }

        _pump = new CancellationTokenSource();
        _ = PumpAsync(_process.StandardOutput.BaseStream, _pump.Token);
        _ = PumpAsync(_process.StandardError.BaseStream, _pump.Token);
    }

    public void Cancel()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        if (_process is not null)
            _process.Exited -= OnExited;

        _pump?.Cancel();
        _pump?.Dispose();
        _pump = null;

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
            }
        }

        _process?.Dispose();
        _process = null;
    }

    void OnExited(object? sender, EventArgs e)
    {
        var code = 0;
        try
        {
            code = _process?.ExitCode ?? 0;
        }
        catch
        {
        }

        // Append short log after status (best-effort; sync, no second Process events).
        try
        {
            var cwd = _process?.StartInfo.WorkingDirectory ?? Environment.CurrentDirectory;
            AppendLogTail(cwd);
        }
        catch
        {
        }

        Exited?.Invoke(code);
    }

    void AppendLogTail(string cwd)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = cwd,
        };
        psi.ArgumentList.Add("-C");
        psi.ArgumentList.Add(cwd);
        psi.ArgumentList.Add("log");
        psi.ArgumentList.Add("-5");
        psi.ArgumentList.Add("--oneline");
        psi.Environment["TERM"] = "dumb";
        psi.Environment["GIT_PAGER"] = "cat";

        using var p = Process.Start(psi);
        if (p is null)
            return;

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(15_000);
        TextReceived?.Invoke("\n── log -5 --oneline ──\n");
        if (!string.IsNullOrWhiteSpace(stdout))
            TextReceived?.Invoke(StripAnsi(stdout));
        if (!string.IsNullOrWhiteSpace(stderr))
            TextReceived?.Invoke(StripAnsi(stderr));
    }

    async Task PumpAsync(Stream stream, CancellationToken ct)
    {
        var buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var n = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                if (n == 0)
                    break;

                TextReceived?.Invoke(StripAnsi(Encoding.UTF8.GetString(buffer, 0, n)));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    static string StripAnsi(string s) => AnsiCsi.Replace(s, string.Empty);

    static string ResolveCwd(string? workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(workingDirectory))
        {
            try
            {
                var full = Path.GetFullPath(workingDirectory.Trim());
                if (Directory.Exists(full))
                    return full;
            }
            catch
            {
            }
        }

        return Environment.CurrentDirectory;
    }
}
