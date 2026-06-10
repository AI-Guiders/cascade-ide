using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Terminal.DataAcquisition;

/// <summary>Lifecycle wrapper for ConPTY/redirected integrated shell session.</summary>
[IoBoundary]
internal sealed class IntegratedTerminalSessionHost : IDisposable
{
    private readonly Func<string?> _getSolutionPath;
    private IIntegratedShellSession? _session;
    private bool _disposed;

    public IntegratedTerminalSessionHost(Func<string?> getSolutionPath) =>
        _getSolutionPath = getSolutionPath;

    public event Action<byte[]>? OutputReceived;

    public event Action<int>? SessionExited;

    public string? ActiveShellDisplayName { get; private set; }

    public void EnsureStarted()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_session is not null)
            return;

        var workingDirectory = IntegratedShellLaunch.ResolveWorkingDirectory(_getSolutionPath());
        var launch = IntegratedShellLaunch.ResolveLaunchConfiguration(workingDirectory);
        ActiveShellDisplayName = launch.DisplayName;

        _session = IntegratedShellLaunch.CreateSession(launch);
        _session.DataReceived += OnDataReceived;
        _session.Exited += OnSessionExited;
    }

    public void SendInput(byte[] input)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (input.Length == 0)
            return;

        EnsureStarted();
        _session!.Send(input);
    }

    public void Resize(int cols, int rows)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_session is null || cols <= 0 || rows <= 0)
            return;

        _session.Resize(cols, rows);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_session is not null)
        {
            _session.DataReceived -= OnDataReceived;
            _session.Exited -= OnSessionExited;
            _session.Dispose();
            _session = null;
        }
    }

    private void OnDataReceived(byte[] data)
    {
        if (data.Length == 0)
            return;

        OutputReceived?.Invoke(data);
    }

    private void OnSessionExited(int exitCode)
    {
        SessionExited?.Invoke(exitCode);
        if (_session is not null)
        {
            _session.DataReceived -= OnDataReceived;
            _session.Exited -= OnSessionExited;
            _session.Dispose();
            _session = null;
        }
    }
}
