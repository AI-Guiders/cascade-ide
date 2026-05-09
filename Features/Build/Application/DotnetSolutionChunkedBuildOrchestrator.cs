using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Build.Application;

/// <summary>
/// Сборка решения с потоковым выводом в канал и сливом в произвольный <see cref="Action{T}"/>.
/// </summary>
[ApplicationOrchestrator]
public static class DotnetSolutionChunkedBuildOrchestrator
{
    /// <summary>
    /// Запускает <c>dotnet build</c> для <paramref name="solutionPath"/> с записью лога по чанкам.
    /// </summary>
    /// <returns>При ошибке CLR — код выхода <c>null</c>, сборка считается неуспешной.</returns>
    public static async Task<(int? ExitCode, bool? BuildSucceeded)> RunSolutionBuildStreamingAsync(
        string solutionPath,
        IDotnetCommandRunner runner,
        Action<string> appendChunk,
        CancellationToken cancellationToken)
    {
        var workDir = Path.GetDirectoryName(solutionPath) ?? "";
        int? lastExitCode = null;
        bool? lastBuildSucceeded = null;

        try
        {
            var channel = BuildLogIngestion.CreateBuildLogChannel();

            var drainTask = BuildLogIngestion.DrainToAppendAsync(
                channel.Reader,
                appendChunk,
                maxBatchChars: 8192,
                cancellationToken: cancellationToken);

            var runTask = runner.RunWithChunkWriterAsync(
                ["build", solutionPath],
                workDir,
                channel.Writer,
                cancellationToken);

            await Task.WhenAll(drainTask, runTask).ConfigureAwait(false);
            var (success, exitCode) = await runTask.ConfigureAwait(false);
            lastExitCode = exitCode;
            lastBuildSucceeded = success;

            appendChunk("\r\n");
            if (!success && exitCode != 0)
                appendChunk($"\r\nКод выхода: {exitCode}");
        }
        catch (Exception ex)
        {
            appendChunk("Ошибка: " + ex.Message + "\r\n");
            lastBuildSucceeded = false;
        }

        return (lastExitCode, lastBuildSucceeded);
    }
}
