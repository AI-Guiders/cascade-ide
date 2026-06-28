using System.Diagnostics;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Открыть путь в проводнике Windows (ADR 0167 §2.7).</summary>
public static class WindowsShellReveal
{
    public static bool TryRevealInExplorer(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            var full = Path.GetFullPath(path.Trim());
            if (!File.Exists(full) && !Directory.Exists(full))
                return false;

            var argument = File.Exists(full)
                ? $"/select,\"{full}\""
                : $"\"{full}\"";

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = argument,
                UseShellExecute = true,
            });
            return process is not null;
        }
        catch
        {
            return false;
        }
    }
}
