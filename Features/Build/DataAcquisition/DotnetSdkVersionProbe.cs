#nullable enable
using System.Diagnostics;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Build.DataAcquisition;

/// <summary>
/// DAL: однократный запуск <c>dotnet --version</c> для индикаторов готовности окружения.
/// </summary>
[IoBoundary]
public static class DotnetSdkVersionProbe
{
    public static async Task<DotnetSdkVersionProbeResult> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet")
            {
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                return new DotnetSdkVersionProbeResult(DotnetSdkVersionProbeOutcome.ProcessNull, null, 0, "", null);

            var outTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            var ver = (await outTask.ConfigureAwait(false)).Trim();
            var err = (await errTask.ConfigureAwait(false)).Trim();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(ver))
                return new DotnetSdkVersionProbeResult(DotnetSdkVersionProbeOutcome.Success, ver, 0, err, null);

            return new DotnetSdkVersionProbeResult(
                DotnetSdkVersionProbeOutcome.NonZeroExit,
                null,
                process.ExitCode,
                err,
                null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DotnetSdkVersionProbeResult(DotnetSdkVersionProbeOutcome.Exception, null, 0, "", ex.Message);
        }
    }
}

public enum DotnetSdkVersionProbeOutcome
{
    ProcessNull,
    Success,
    NonZeroExit,
    Exception,
}

public readonly record struct DotnetSdkVersionProbeResult(
    DotnetSdkVersionProbeOutcome Outcome,
    string? Version,
    int ExitCode,
    string StdErr,
    string? ExceptionMessage);
