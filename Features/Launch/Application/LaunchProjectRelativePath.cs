using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Launch.Application;

/// <summary>Путь к .csproj относительно корня каталога решения.</summary>
public static class LaunchProjectRelativePath
{
    public static bool TryGetRelativeToSolutionDirectory(
        string solutionRootDirectory,
        string csprojFullPath,
        [NotNullWhen(true)] out string? relativePath,
        [NotNullWhen(false)] out string? error)
    {
        relativePath = null;
        error = null;
        try
        {
            var rel = Path.GetRelativePath(solutionRootDirectory, CanonicalFilePath.Normalize(csprojFullPath));
            if (rel.StartsWith("..", StringComparison.Ordinal))
            {
                error = "Проект должен быть внутри каталога решения.";
                return false;
            }

            relativePath = rel;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }
}
