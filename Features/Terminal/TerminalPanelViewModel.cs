using System.Diagnostics;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Terminal;

/// <summary>
/// Вкладка «Terminal» нижней панели: вывод и ввод команд в рабочем каталоге решения.
/// </summary>
public partial class TerminalPanelViewModel : ViewModelBase
{
    private readonly Func<string?> _getSolutionPath;

    public TerminalPanelViewModel(Func<string?> getSolutionPath)
    {
        _getSolutionPath = getSolutionPath;
    }

    [ObservableProperty]
    private string _terminalOutput = "";

    [ObservableProperty]
    private string _terminalInput = "";

    [RelayCommand]
    private async Task RunTerminalCommandAsync()
    {
        var cmd = TerminalInput?.Trim() ?? "";
        if (string.IsNullOrEmpty(cmd))
            return;
        TerminalInput = "";
        var solutionPath = _getSolutionPath();
        var workDir = !string.IsNullOrWhiteSpace(solutionPath) && File.Exists(solutionPath)
            ? Path.GetDirectoryName(solutionPath) ?? Environment.CurrentDirectory
            : Environment.CurrentDirectory;
        TerminalOutput += $"> {cmd}\r\n";
        try
        {
            var isWin = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var psi = new ProcessStartInfo(isWin ? "cmd" : "sh")
            {
                ArgumentList = { isWin ? "/c" : "-c", cmd },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };
            using var process = Process.Start(psi);
            if (process is null)
            {
                TerminalOutput += "Не удалось запустить процесс.\r\n";
                return;
            }
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(true);
            await process.WaitForExitAsync().ConfigureAwait(true);
            var outStr = await stdout;
            var errStr = await stderr;
            if (outStr.Length > 0) TerminalOutput += outStr;
            if (errStr.Length > 0) TerminalOutput += errStr;
            if (process.ExitCode != 0)
                TerminalOutput += $"\r\nExit code: {process.ExitCode}\r\n";
        }
        catch (Exception ex)
        {
            TerminalOutput += ex.Message + "\r\n";
        }
    }
}
