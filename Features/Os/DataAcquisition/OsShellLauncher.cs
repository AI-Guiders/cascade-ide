#nullable enable
using System.Diagnostics;
using System.IO;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Os.DataAcquisition;

[IoBoundary]
public interface IOsShellLauncher
{
    void TryOpen(string target, Action<string>? onError = null);
    void TryOpenDirectory(string directoryPath, Action<string>? onError = null);
    void TryOpenUrl(string url, Action<string>? onError = null);
    void TryRevealFile(string filePath, Action<string>? onError = null);
}

/// <summary>
/// DAL: centralized OS-shell interactions (open path/url with default handler).
/// Cross-feature dependency point for "ask OS to open something".
/// </summary>
[IoBoundary]
public sealed class OsShellLauncher : IOsShellLauncher
{
    public void TryOpen(string target, Action<string>? onError = null)
    {
        if (string.IsNullOrWhiteSpace(target))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public void TryOpenDirectory(string directoryPath, Action<string>? onError = null) =>
        TryOpen(directoryPath, onError);

    public void TryOpenUrl(string url, Action<string>? onError = null) =>
        TryOpen(url, onError);

    public void TryRevealFile(string filePath, Action<string>? onError = null)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        try
        {
            var full = CanonicalFilePath.Normalize(filePath);
            var dir = Path.GetDirectoryName(full);
            if (string.IsNullOrWhiteSpace(dir))
            {
                TryOpen(full, onError);
                return;
            }

            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{full}\"",
                    UseShellExecute = true
                });
                return;
            }

            if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"-R \"{full}\"",
                    UseShellExecute = true
                });
                return;
            }

            TryOpen(dir, onError);
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }
}

/// <summary>Access to the default OS shell launcher (until full DI is introduced).</summary>
[IoBoundary]
public static class OsShell
{
    public static IOsShellLauncher Default { get; } = new OsShellLauncher();
}

