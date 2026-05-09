#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace CascadeIDE.Features.Launch.DataAcquisition;

/// <summary>
/// DAL external adapter: резолв и проверка пути к .csproj в файловой системе.
/// </summary>
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
}
