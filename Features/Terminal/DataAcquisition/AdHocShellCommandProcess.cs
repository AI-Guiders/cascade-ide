#nullable enable
using System.Diagnostics;

namespace CascadeIDE.Features.Terminal.DataAcquisition;

/// <summary>
/// DAL: однократный запуск <c>cmd /c</c> или <c>sh -c</c> в каталоге для нижней панели «Terminal».
/// </summary>
public static class AdHocShellCommandProcess
{
    public static Process? TryStart(string workingDirectory, string userCommand, out string? startError)
    {
        startError = null;
        try
        {
            var isWin = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var psi = new ProcessStartInfo(isWin ? "cmd" : "sh")
            {
                ArgumentList = { isWin ? "/c" : "-c", userCommand },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };
            return Process.Start(psi);
        }
        catch (Exception ex)
        {
            startError = ex.Message;
            return null;
        }
    }
}
