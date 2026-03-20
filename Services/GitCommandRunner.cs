using System.Diagnostics;

namespace CascadeIDE.Services;

/// <inheritdoc cref="IGitCommandRunner" />
public sealed class GitCommandRunner : IGitCommandRunner
{
    public async Task<(bool Success, int ExitCode, string Output)> RunAsync(
        IReadOnlyList<string> args,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            return (false, -1, "Workspace path is not available.");

        try
        {
            var psi = new ProcessStartInfo("git")
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
                return (false, -1, "Failed to start git process.");

            var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            var output = (await stdout) + "\n" + (await stderr);
            return (process.ExitCode == 0, process.ExitCode, output.Trim());
        }
        catch (Exception ex)
        {
            return (false, -1, ex.Message);
        }
    }
}
