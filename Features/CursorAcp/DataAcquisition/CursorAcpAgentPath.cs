using CascadeIDE.Contracts;
namespace CascadeIDE.Features.CursorAcp.DataAcquisition;

/// <summary>Сопоставление пути из настроек с <c>cursor-agent.cmd</c> из пакета Cursor ACP.</summary>
[IoBoundary]
public static class CursorAcpAgentPath
{
    /// <summary>Возвращает полный путь к cmd и рабочий каталог для процесса.</summary>
    public static bool TryResolve(string? configured, out string cmdPath, out string workingDirectory)
    {
        cmdPath = "";
        workingDirectory = "";
        if (string.IsNullOrWhiteSpace(configured))
            return TryResolveFromPath(out cmdPath, out workingDirectory);

        var trimmed = configured.Trim();
        if (File.Exists(trimmed))
        {
            var ext = Path.GetExtension(trimmed);
            if (string.Equals(ext, ".cmd", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".bat", StringComparison.OrdinalIgnoreCase))
            {
                cmdPath = CanonicalFilePath.Normalize(trimmed);
                workingDirectory = Path.GetDirectoryName(cmdPath) ?? "";
                return true;
            }
        }

        if (!Directory.Exists(trimmed))
            return false;

        var dir = CanonicalFilePath.Normalize(trimmed);
        foreach (var rel in new[] { Path.Combine("dist-package", "cursor-agent.cmd"), "cursor-agent.cmd" })
        {
            var p = Path.Combine(dir, rel);
            if (File.Exists(p))
            {
                cmdPath = CanonicalFilePath.Normalize(p);
                workingDirectory = Path.GetDirectoryName(cmdPath) ?? dir;
                return true;
            }
        }

        return false;
    }

    private static bool TryResolveFromPath(out string cmdPath, out string workingDirectory)
    {
        cmdPath = "";
        workingDirectory = "";

        foreach (var candidate in new[] { "cursor-agent", "cursor-agent.cmd", "cursor-agent.bat" })
        {
            var resolved = EnvironmentReadinessExecutablePathProbe.TryResolveExecutablePath(candidate);
            if (string.IsNullOrWhiteSpace(resolved))
                continue;

            cmdPath = resolved;
            workingDirectory = Path.GetDirectoryName(resolved) ?? "";
            return true;
        }

        return false;
    }
}
