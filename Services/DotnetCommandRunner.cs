using System.Diagnostics;

namespace CascadeIDE.Services;

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
}

