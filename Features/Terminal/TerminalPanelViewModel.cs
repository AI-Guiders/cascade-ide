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

    private static string ResolveTerminalWorkingDirectory(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return Environment.CurrentDirectory;
        try
        {
            var p = Path.GetFullPath(solutionPath.Trim());
            if (File.Exists(p))
                return Path.GetDirectoryName(p) ?? Environment.CurrentDirectory;
            if (Directory.Exists(p))
                return p;
        }
        catch
        {
            // fall through
        }

        return Environment.CurrentDirectory;
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
        var workDir = ResolveTerminalWorkingDirectory(solutionPath);
        AppendOutput($"> {cmd}\r\n");
        try
        {
            using var process = AdHocShellCommandProcess.TryStart(workDir, cmd, out var startError);
            if (process is null)
            {
                AppendOutput((startError is not null ? startError + "\r\n" : "") + "Не удалось запустить процесс.\r\n");
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
