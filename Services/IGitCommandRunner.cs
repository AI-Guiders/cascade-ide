namespace CascadeIDE.Services;

/// <summary>
/// Запуск <c>git</c> в указанном рабочем каталоге. Общий для Git-панели, телеметрии и MCP.
/// </summary>
public interface IGitCommandRunner
{
    Task<(bool Success, int ExitCode, string Output)> RunAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}
