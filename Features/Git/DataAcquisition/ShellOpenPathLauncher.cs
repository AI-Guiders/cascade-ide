#nullable enable
using System.Diagnostics;

namespace CascadeIDE.Features.Git.DataAcquisition;

/// <summary>DAL: открытие пути в оболочке ОС (например папка в Explorer).</summary>
public static class ShellOpenPathLauncher
{
    public static void TryOpenInDefaultShell(string fullPath, Action<string>? onError = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = fullPath, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }
}
