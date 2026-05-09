#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Contracts;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Launch.Application;

/// <summary>
/// Эвристика стартового <c>.csproj</c> перед F5, если в памяти путь пуст или файл не найден на диске
/// (<see cref="McpSolutionTree"/> · обозреватель решения).
/// </summary>
[ComputingUnit("startup-project-f5-infer")]
public static class StartupProjectDebugInferenceProjection
{
    public static bool HasPersistedStartupPointingToExistingFile(string? startupCsprojFullPath) =>
        !string.IsNullOrEmpty(startupCsprojFullPath) && File.Exists(startupCsprojFullPath);

    /// <summary>
    /// Подбирает полный канонический путь к управляемому проекту или <see langword="null"/>.
    /// </summary>
    public static string? TryInferCanonicalCsproj(
        ObservableCollection<SolutionItem> solutionRoots,
        string? currentFilePath,
        string? selectedSolutionItemFullPath)
    {
        if (solutionRoots.Count == 0)
            return null;

        var csprojs = McpSolutionTree.CollectDistinctManagedProjectPaths(solutionRoots);
        var set = csprojs.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (csprojs.Count == 1)
            return csprojs[0];

        if (!string.IsNullOrEmpty(currentFilePath))
        {
            try
            {
                var full = CanonicalFilePath.Normalize(currentFilePath);
                if (File.Exists(full) && !McpSolutionTree.IsBuildArtifactPath(full))
                {
                    if (McpSolutionTree.MapFileToProject(solutionRoots).TryGetValue(full, out var treeProj) &&
                        !string.IsNullOrEmpty(treeProj) && set.Contains(treeProj))
                        return treeProj;

                    var disk = McpSolutionTree.ResolveOwningProjectPath(full);
                    if (!string.IsNullOrEmpty(disk) && set.Contains(disk))
                        return disk;
                }
            }
            catch
            {
                // как в VM: тихо, идём к выбору из обозревателя
            }
        }

        var sel = selectedSolutionItemFullPath;
        if (!string.IsNullOrEmpty(sel) &&
            (sel.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
             sel.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase)) &&
            File.Exists(sel))
        {
            var p = CanonicalFilePath.Normalize(sel);
            if (set.Contains(p))
                return p;
        }

        return null;
    }
}
