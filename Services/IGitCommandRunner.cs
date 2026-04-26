namespace CascadeIDE.Services;

/// <summary>
/// Порт: запуск <c>git</c> в указанном рабочем каталоге. Общий для Git-панели, телеметрии и MCP.
/// Реализация (DAL, внешний процесс): <c>CascadeIDE.Features.Git.DataAcquisition.GitCommandRunner</c>.
/// </summary>
public interface IGitCommandRunner
{
    Task<(bool Success, int ExitCode, string Output)> RunAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}
