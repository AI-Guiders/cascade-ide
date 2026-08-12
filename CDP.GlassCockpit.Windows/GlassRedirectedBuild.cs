#nullable enable

using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CascadeIDE.SoftInstrument;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Thin redirected <c>dotnet build</c> log for Glass Build MFD (not Avalonia MSBuild host).
/// Full build SSOT remains Avalonia BuildMfdPageView + BuildOutputPanelViewModel.
/// </summary>
internal sealed class GlassRedirectedBuild : IDisposable
{
    static readonly Regex AnsiCsi = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]|\x1B\].*?\x07|\x1B\].*?\x1B\\", RegexOptions.Compiled);

    Process? _process;
    CancellationTokenSource? _pump;

    public event Action<string>? TextReceived;
    public event Action<int>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public string DisplayTarget { get; private set; } = "?";

    public void Start(string workingDirectory, string? solutionOrProjectPath = null)
    {
        if (IsRunning)
            return;

        var cwd = ResolveCwd(workingDirectory);
        var target = ResolveBuildTarget(cwd, solutionOrProjectPath);
        DisplayTarget = Path.GetFileName(target);

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = cwd,
        };
        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(target);
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("--verbosity");
        startInfo.ArgumentList.Add("minimal");
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en-US";
        startInfo.Environment["TERM"] = "dumb";

        _process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        _process.Exited += OnExited;

        TextReceived?.Invoke($"┌ Glass redirected · dotnet build · {DisplayTarget}\n");
        TextReceived?.Invoke($"│ cwd {cwd}\n");

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
                // best-effort
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

    /// <summary>Prefer open <paramref name="solutionOrProjectPath"/> (CIDE session SSOT), then glance sln under cwd.</summary>
    static string ResolveBuildTarget(string cwd, string? solutionOrProjectPath)
    {
        if (!string.IsNullOrWhiteSpace(solutionOrProjectPath))
        {
            try
            {
                var full = Path.GetFullPath(solutionOrProjectPath.Trim());
                if (File.Exists(full) && IsBuildableProjectFile(full))
                    return full;
            }
            catch
            {
            }
        }

        return GlassSolutionExplorerGlance.TryResolveSlnPath(cwd)
               ?? Path.Combine(cwd, "CascadeIDE.sln");
    }

    static bool IsBuildableProjectFile(string path) =>
        path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase);
}
