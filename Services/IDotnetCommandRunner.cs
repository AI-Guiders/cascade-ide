using System.Threading.Channels;

namespace CascadeIDE.Services;

/// <summary>
/// Порт: запуск <c>dotnet</c> CLI в указанном рабочем каталоге.
/// Реализация (DAL, внешний процесс): <c>CascadeIDE.Features.Build.DataAcquisition.DotnetCommandRunner</c>.
/// </summary>
public interface IDotnetCommandRunner
{
    Task<(bool Success, int ExitCode, string Output)> RunAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Потоковый вывод: куски stdout/stderr (в неопределённом взаимном порядке между потоками) пишет в
    /// <paramref name="chunkWriter"/>; по завершении процесса вызывает
    /// <see cref="ChannelWriter{T}.TryComplete()"/> (с исключением при сбое старта/ожидания).
    /// Писатель обычно сидит на <see cref="System.Threading.Channels.BoundedChannelOptions"/> с
    /// <c>FullMode = Wait</c> (см. <c>BuildLogIngestion.CreateBuildLogChannel</c>, ADR 0094).
    /// </summary>
    Task<(bool Success, int ExitCode)> RunWithChunkWriterAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        ChannelWriter<string> chunkWriter,
        CancellationToken cancellationToken = default);
}

