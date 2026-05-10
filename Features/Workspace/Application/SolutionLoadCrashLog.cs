#nullable enable
using System.Globalization;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// Локальный журнал при необработанном исключении загрузки решения (<c>.cascade-ide/crash-log.txt</c> рядом с .sln или в текущем каталоге).
/// </summary>
[ComputingUnit("workspace-solution-load-crash-log")]
public static class SolutionLoadCrashLog
{
    public static void TryAppend(string? solutionPath, Exception ex)
    {
        try
        {
            var baseDir = "";
            if (!string.IsNullOrWhiteSpace(solutionPath))
            {
                try
                {
                    var full = CanonicalFilePath.Normalize(solutionPath);
                    baseDir = File.Exists(full) ? (Path.GetDirectoryName(full) ?? "") : full;
                }
                catch
                {
                    baseDir = "";
                }
            }

            if (string.IsNullOrWhiteSpace(baseDir))
                baseDir = Environment.CurrentDirectory;

            var logDir = Path.Combine(baseDir, ".cascade-ide");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "crash-log.txt");
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff 'UTC'", CultureInfo.InvariantCulture);
            var payload =
                $"[{stamp}] LoadSolution crash{Environment.NewLine}" +
                $"solution: {solutionPath}{Environment.NewLine}" +
                $"{ex}{Environment.NewLine}" +
                $"---{Environment.NewLine}";
            File.AppendAllText(logPath, payload);
        }
        catch
        {
            // Do not throw from crash logger.
        }
    }
}
