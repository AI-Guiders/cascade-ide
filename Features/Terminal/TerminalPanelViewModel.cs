using System.Diagnostics;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Terminal;

/// <summary>
/// Вкладка «Terminal» нижней панели: вывод и ввод команд в рабочем каталоге решения.
/// </summary>
public partial class TerminalPanelViewModel : ViewModelBase
{
    public const int MaxChars = 250_000;

    private readonly Func<string?> _getSolutionPath;
    private OutputAccumulator _acc = new(MaxChars);

    public TerminalPanelViewModel(Func<string?> getSolutionPath)
    {
        _getSolutionPath = getSolutionPath;
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
        AppendOutput($"> {cmd}\r\n");
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
                AppendOutput("Не удалось запустить процесс.\r\n");
                return;
            }

            async Task PumpAsync(StreamReader reader)
            {
                var buffer = new char[4096];
                while (true)
                {
                    var read = await reader.ReadAsync(buffer).ConfigureAwait(true);
                    if (read <= 0)
                        break;
                    AppendOutput(new string(buffer, 0, read));
                }
            }

            var pumpOut = PumpAsync(process.StandardOutput);
            var pumpErr = PumpAsync(process.StandardError);

            await Task.WhenAll(pumpOut, pumpErr).ConfigureAwait(true);
            await process.WaitForExitAsync().ConfigureAwait(true);

            if (process.ExitCode != 0)
                AppendOutput($"\r\nExit code: {process.ExitCode}\r\n");
        }
        catch (Exception ex)
        {
            AppendOutput(ex.Message + "\r\n");
        }
    }
}
