namespace CascadeIDE.Services;

/// <summary>
/// Запуск <c>dotnet</c> CLI в указанном рабочем каталоге.
/// </summary>
public interface IDotnetCommandRunner
{
    Task<(bool Success, int ExitCode, string Output)> RunAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}

