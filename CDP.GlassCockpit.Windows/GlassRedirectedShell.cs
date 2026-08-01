#nullable enable

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Thin redirected Process shell for Glass Terminal MFD (not ConPTY).
/// Full TTY SSOT remains Avalonia TerminalMfdPageView + IntegratedShellLaunch.
/// </summary>
internal sealed class GlassRedirectedShell : IDisposable
{
    static readonly Regex AnsiCsi = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]|\x1B\].*?\x07|\x1B\].*?\x1B\\", RegexOptions.Compiled);

    Process? _process;
    CancellationTokenSource? _pump;

    public event Action<string>? TextReceived;
    public event Action<int>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public string DisplayName { get; private set; } = "?";

    public void Start(string workingDirectory)
    {
        if (IsRunning)
            return;

        var cwd = ResolveCwd(workingDirectory);
        var (fileName, args, display) = ResolveLaunch(cwd);
        DisplayName = display;

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = Encoding.UTF8,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = cwd,
        };
        foreach (var a in args)
            startInfo.ArgumentList.Add(a);

        startInfo.Environment["TERM"] = "dumb";
        startInfo.Environment["COLORTERM"] = "";
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnExited;
        _process.Start();

        _pump = new CancellationTokenSource();
        _ = PumpAsync(_process.StandardOutput.BaseStream, _pump.Token);
        _ = PumpAsync(_process.StandardError.BaseStream, _pump.Token);

        TextReceived?.Invoke($"┌ Glass redirected · {display} · {cwd}\n");
    }

    public void SendLine(string line)
    {
        try
        {
            var input = _process?.StandardInput;
            if (input is null)
                return;
            input.WriteLine(line);
            input.Flush();
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public void Dispose()
    {
        if (_process is not null)
            _process.Exited -= OnExited;

        _pump?.Cancel();
        _pump?.Dispose();
        _pump = null;

        try
        {
            _process?.StandardInput.Close();
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch
            {
                // best-effort
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

        Exited?.Invoke(code);
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

                var raw = Encoding.UTF8.GetString(buffer, 0, n);
                TextReceived?.Invoke(StripAnsi(raw));
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

    static (string FileName, string[] Args, string Display) ResolveLaunch(string cwd)
    {
        foreach (var candidate in new[]
                 {
                     @"C:\Program Files\PowerShell\7\pwsh.exe",
                     Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "PowerShell", "7", "pwsh.exe"),
                 })
        {
            if (File.Exists(candidate))
                return (candidate, ["-NoLogo", "-NoExit"], "pwsh");
        }

        var comSpec = Environment.GetEnvironmentVariable("ComSpec");
        if (!string.IsNullOrWhiteSpace(comSpec) && File.Exists(comSpec))
            return (comSpec, ["/K", "chcp 65001>nul"], Path.GetFileName(comSpec));

        return ("cmd.exe", ["/K", "chcp 65001>nul"], "cmd.exe");
    }
}
