using AvaloniaTerminal;
using CascadeIDE.Features.Terminal.DataAcquisition;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Terminal;

/// <summary>
/// Вкладка «Terminal» нижней панели: интерактивная shell-сессия (ConPTY на Windows, redirected fallback)
/// через <see cref="TerminalControlModel"/> (ANSI, сетка, сырой TTY-ввод).
/// </summary>
public sealed class TerminalPanelViewModel : ViewModelBase, IDisposable
{
    private const int ScrollbackLines = 10_000;
    private static readonly TimeSpan BackspaceRepeatMinInterval = TimeSpan.FromMilliseconds(50);

    private readonly IntegratedTerminalSessionHost _shellHost;
    private readonly IntegratedShellBackspaceBurstGuard _backspaceBurstGuard = new();
    private bool _disposed;
    private bool _sessionStarted;
    private (int cols, int rows)? _pendingTerminalSize;
    private DateTime _lastPureBackspaceSentUtc = DateTime.MinValue;
    private bool _shellOutputLeadingBomStripped;

    public TerminalPanelViewModel(Func<string?> getSolutionPath)
    {
        _shellHost = new IntegratedTerminalSessionHost(getSolutionPath);
        TerminalModel = new TerminalControlModel(new TerminalOptions
        {
            ReflowOnResize = false,
            Scrollback = ScrollbackLines,
        });

        TerminalModel.UserInput += OnTerminalUserInput;
        TerminalModel.SizeChanged += OnTerminalSizeChanged;
        _shellHost.OutputReceived += OnShellOutput;
        _shellHost.SessionExited += OnShellSessionExited;
    }

    public TerminalControlModel TerminalModel { get; }

    public void EnsureSessionStarted()
    {
        if (_disposed || _sessionStarted)
            return;

        try
        {
            _shellHost.EnsureStarted();
            _sessionStarted = true;
            _backspaceBurstGuard.Reset();
            _shellOutputLeadingBomStripped = false;
            ApplyPendingResize();
        }
        catch (Exception ex)
        {
            FeedOnUiThread(ex.Message + "\r\n");
        }
    }

    public void Clear() => FeedOnUiThread("\x1b[2J\x1b[H");

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        FeedOnUiThread(text);
    }

    private void OnTerminalUserInput(object? sender, TerminalUserInputEventArgs e)
    {
        if (_disposed || e.Data.Length == 0)
            return;

        var rawInput = e.Data.ToArray();
        if (IntegratedShellLaunch.IsPureBackspaceInput(rawInput))
        {
            var now = DateTime.UtcNow;
            if (now - _lastPureBackspaceSentUtc < BackspaceRepeatMinInterval)
                return;

            _lastPureBackspaceSentUtc = now;
        }

        var filteredInput = _backspaceBurstGuard.FilterUserInput(rawInput);
        if (filteredInput.Length == 0)
            return;

        try
        {
            EnsureSessionStarted();
            _shellHost.SendInput(filteredInput);
        }
        catch (Exception ex)
        {
            FeedOnUiThread(ex.Message + "\r\n");
        }
    }

    private void OnTerminalSizeChanged(object? sender, TerminalSizeChangedEventArgs e)
    {
        if (_disposed || e.Cols <= 0 || e.Rows <= 0)
            return;

        _pendingTerminalSize = (e.Cols, e.Rows);
        if (_sessionStarted)
            _shellHost.Resize(e.Cols, e.Rows);
    }

    private void OnShellOutput(byte[] data)
    {
        if (data.Length == 0)
            return;

        _backspaceBurstGuard.NotifyShellOutput();
        var sanitized = IntegratedShellStreamSanitizer.SanitizeShellOutput(data, ref _shellOutputLeadingBomStripped);
        if (sanitized.Length == 0)
            return;

        if (UiScheduler.Default.CheckAccess())
        {
            TerminalModel.Feed(sanitized, sanitized.Length);
            return;
        }

        UiScheduler.Default.Post(() =>
        {
            if (_disposed)
                return;

            TerminalModel.Feed(sanitized, sanitized.Length);
        });
    }

    private void OnShellSessionExited(int exitCode)
    {
        _sessionStarted = false;
        _backspaceBurstGuard.Reset();
        if (exitCode != 0)
            FeedOnUiThread($"\r\nShell exited: {exitCode}\r\n");
    }

    private void ApplyPendingResize()
    {
        if (_pendingTerminalSize is not { } size)
            return;

        _shellHost.Resize(size.cols, size.rows);
    }

    private void FeedOnUiThread(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (UiScheduler.Default.CheckAccess())
        {
            TerminalModel.Feed(text);
            return;
        }

        UiScheduler.Default.Post(() =>
        {
            if (_disposed)
                return;

            TerminalModel.Feed(text);
        });
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        TerminalModel.UserInput -= OnTerminalUserInput;
        TerminalModel.SizeChanged -= OnTerminalSizeChanged;
        _shellHost.OutputReceived -= OnShellOutput;
        _shellHost.SessionExited -= OnShellSessionExited;
        _shellHost.Dispose();
    }
}
