using CascadeIDE.Features.Terminal.DataAcquisition;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Terminal;

/// <summary>
/// Вкладка «Terminal» нижней панели: интерактивная shell-сессия (ConPTY на Windows, redirected fallback).
/// </summary>
public partial class TerminalPanelViewModel : ViewModelBase, IDisposable
{
    public const int MaxChars = 250_000;

    private readonly Func<string?> _getSolutionPath;
    private readonly IntegratedTerminalSessionHost _shellHost;
    private OutputAccumulator _acc = new(MaxChars);
    private bool _disposed;

    public TerminalPanelViewModel(Func<string?> getSolutionPath)
    {
        _getSolutionPath = getSolutionPath;
        _shellHost = new IntegratedTerminalSessionHost(getSolutionPath);
        _shellHost.OutputReceived += AppendOutput;
        _shellHost.SessionExited += OnShellSessionExited;
    }

    [ObservableProperty]
    private string _terminalOutput = "";

    [ObservableProperty]
    private string _terminalInput = "";

    public void Clear()
    {
        _acc = new OutputAccumulator(MaxChars);
        TerminalOutput = "";
    }

    public void AppendOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        _acc.Append(text.AsSpan());
        TerminalOutput = _acc.ToStringAndTrim();
    }

    [RelayCommand]
    private Task RunTerminalCommandAsync()
    {
        var cmd = TerminalInput?.Trim() ?? "";
        if (string.IsNullOrEmpty(cmd))
            return Task.CompletedTask;

        TerminalInput = "";
        try
        {
            _shellHost.EnsureStarted();
            if (_shellHost.ActiveShellDisplayName is { } shellName
                && TerminalOutput.Length == 0)
            {
                var workDir = IntegratedShellLaunch.ResolveWorkingDirectory(_getSolutionPath());
                AppendOutput($"[{shellName} · {workDir}]\r\n");
            }

            AppendOutput($"> {cmd}\r\n");
            _shellHost.SendCommandLine(cmd);
        }
        catch (Exception ex)
        {
            AppendOutput(ex.Message + "\r\n");
        }

        return Task.CompletedTask;
    }

    private void OnShellSessionExited(int exitCode)
    {
        if (exitCode != 0)
            AppendOutput($"\r\nShell exited: {exitCode}\r\n");
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _shellHost.OutputReceived -= AppendOutput;
        _shellHost.SessionExited -= OnShellSessionExited;
        _shellHost.Dispose();
    }
}
