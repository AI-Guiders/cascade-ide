using System.Diagnostics;
using System.Threading.Channels;

namespace CascadeIDE.Features.Build.DataAcquisition;

/// <summary>
/// DAL: запуск внешнего <c>dotnet</c> CLI в рабочем каталоге (процесс, чтение stdout/stderr, опционально потоковая передача).
/// </summary>
/// <inheritdoc cref="IDotnetCommandRunner" />
public sealed class DotnetCommandRunner : IDotnetCommandRunner
{
    /// <summary>
    /// Safety cap for accumulated process output. Keeps last N chars.
    /// </summary>
    public const int MaxOutputChars = 250_000;

    public async Task<(bool Success, int ExitCode, string Output)> RunAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            return (false, -1, "Working directory is not available.");

        try
        {
            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process is null)
                return (false, -1, "Failed to start dotnet process.");

            var acc = new OutputAccumulator(MaxOutputChars);

            async Task PumpAsync(StreamReader reader)
            {
                var buffer = new char[4096];
                while (true)
                {
                    var read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read <= 0)
                        break;
                    acc.Append(buffer.AsSpan(0, read));
                }
            }

            var pumpOut = PumpAsync(process.StandardOutput);
            var pumpErr = PumpAsync(process.StandardError);

            await Task.WhenAll(pumpOut, pumpErr).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = acc.ToStringAndTrim();
            return (process.ExitCode == 0, process.ExitCode, output);
        }
        catch (Exception ex)
        {
            return (false, -1, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<(bool Success, int ExitCode)> RunWithChunkWriterAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        ChannelWriter<string> chunkWriter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunkWriter);

        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
        {
            chunkWriter.TryComplete();
            return (false, -1);
        }

        try
        {
            var psi = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            foreach (var arg in args)
                psi.ArgumentList.Add(arg);

            using var process = Process.Start(psi);
            if (process is null)
            {
                chunkWriter.TryComplete();
                return (false, -1);
            }

            static void TryKillEntireProcessTree(Process? p)
            {
                if (p is null) return;
                try
                {
                    if (!p.HasExited)
                        p.Kill(true);
                }
                catch
                {
                    // ignore
                }
            }

            using (cancellationToken.Register(() => TryKillEntireProcessTree(process), useSynchronizationContext: false))
            {
                async Task PumpAsync(StreamReader reader)
                {
                    var buffer = new char[4096];
                    while (true)
                    {
                        var read = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                        if (read <= 0)
                            break;
                        var chunk = new string(buffer, 0, read);
                        await chunkWriter.WriteAsync(chunk, cancellationToken).ConfigureAwait(false);
                    }
                }

                var pumpOut = PumpAsync(process.StandardOutput);
                var pumpErr = PumpAsync(process.StandardError);
                try
                {
                    await Task.WhenAll(pumpOut, pumpErr).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    TryKillEntireProcessTree(process);
                    try
                    {
                        chunkWriter.TryComplete(ex);
                    }
                    catch
                    {
                        // channel may be closed
                    }
                    return (false, -1);
                }

                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                return (process.ExitCode == 0, process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            try
            {
                chunkWriter.TryComplete(ex);
            }
            catch
            {
                // channel may be closed
            }
            return (false, -1);
        }
        finally
        {
            try
            {
                _ = chunkWriter.TryComplete();
            }
            catch
            {
                // ignore
            }
        }
    }
}
