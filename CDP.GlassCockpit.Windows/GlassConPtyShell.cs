#nullable enable

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CascadeIDE.Features.Terminal.DataAcquisition;

namespace CDP.GlassCockpit.Windows;

/// <summary>
/// Glass Terminal MFD — shared GlassCore ConPTY/redirected session.
/// TextBox strip is interim; WPF VT control remains depth OPEN.
/// </summary>
internal sealed class GlassConPtyShell : IDisposable
{
    static readonly Regex AnsiCsi = new(@"\x1B\[[0-9;?]*[ -/]*[@-~]|\x1B\].*?\x07|\x1B\].*?\x1B\\", RegexOptions.Compiled);

    IIntegratedShellSession? _session;
    bool _leadingBomStripped;
    bool _alive;

    public event Action<string>? TextReceived;
    public event Action<int>? Exited;

    public bool IsRunning => _alive && _session is not null;

    public string DisplayName { get; private set; } = "?";

    public void Start(string workingDirectory)
    {
        if (IsRunning)
            return;

        DisposeSessionOnly();

        var cwd = IntegratedShellLaunch.ResolveWorkingDirectory(workingDirectory);
        var launch = IntegratedShellLaunch.ResolveLaunchConfiguration(cwd);
        DisplayName = launch.DisplayName;
        _leadingBomStripped = false;

        _session = IntegratedShellLaunch.CreateSession(launch);
        _alive = true;
        _session.DataReceived += OnDataReceived;
        _session.Exited += OnExited;

        TextReceived?.Invoke($"┌ Glass ConPTY · {DisplayName} · {launch.WorkingDirectory}\n");
    }

    public void SendLine(string line)
    {
        if (_session is null || !_alive)
            return;

        try
        {
            var payload = Encoding.UTF8.GetBytes(line + "\n");
            _session.Send(payload);
        }
        catch (ObjectDisposedException)
        {
        }
        catch (IOException)
        {
        }
    }

    public void Dispose()
    {
        DisposeSessionOnly();
    }

    void DisposeSessionOnly()
    {
        if (_session is null)
            return;

        _session.DataReceived -= OnDataReceived;
        _session.Exited -= OnExited;
        _alive = false;
        _session.Dispose();
        _session = null;
    }

    void OnDataReceived(byte[] data)
    {
        if (data.Length == 0)
            return;

        var sanitized = IntegratedShellStreamSanitizer.SanitizeShellOutput(data, ref _leadingBomStripped);
        if (sanitized.Length == 0)
            return;

        var raw = Encoding.UTF8.GetString(sanitized);
        TextReceived?.Invoke(StripAnsi(raw));
    }

    void OnExited(int code)
    {
        _alive = false;
        Exited?.Invoke(code);
    }

    static string StripAnsi(string s) => AnsiCsi.Replace(s, string.Empty);
}
