#nullable enable
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Launch.DataAcquisition;

/// <summary>
/// DAL external adapter: резолв и проверка пути к .csproj в файловой системе.
/// </summary>
[IoBoundary]
public static class LaunchProjectPathResolver
{
    public static bool TryGetExistingCsprojFullPath(
        string solutionDirectory,
        string projectRelativeToSolution,
        [NotNullWhen(true)] out string? csprojFullPath)
    {
        csprojFullPath = null;
        if (string.IsNullOrWhiteSpace(projectRelativeToSolution))
            return false;
        var full = CanonicalFilePath.Normalize(Path.Combine(solutionDirectory, projectRelativeToSolution));
        if (!File.Exists(full))
            return false;
        csprojFullPath = full;
        return true;
    }

    /// <summary>Возвращает нормализованный путь, если файл существует; иначе <see langword="null"/>.</summary>
    public static string? NormalizeExistingProjectFileFullPath(string? projectFullPath)
    {
        if (string.IsNullOrWhiteSpace(projectFullPath))
            return null;
        var full = CanonicalFilePath.Normalize(projectFullPath);
        return File.Exists(full) ? full : null;
    }
}
