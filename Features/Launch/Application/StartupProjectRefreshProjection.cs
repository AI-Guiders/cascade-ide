using CascadeIDE.Contracts;
using CascadeIDE.Features.Launch.DataAcquisition;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Launch.Application;

/// <summary>Восстановление стартового <c>.csproj</c> после загрузки решения (store + единственный проект).</summary>
[ComputingUnit("startup project refresh after solution load")]
public static class StartupProjectRefreshProjection
{
    public sealed record Result(string? CsprojFullPath, string ShortLabel);

    public static Result Empty => new(null, "");

    public static Result ResolveAfterSolutionLoad(
        string? solutionPath,
        System.Collections.ObjectModel.ObservableCollection<SolutionItem> solutionRoots,
        string solutionDirectory)
    {
        if (string.IsNullOrEmpty(solutionPath) || solutionRoots.Count == 0)
            return Empty;

        var projects = McpSolutionTree.CollectProjectPaths(solutionRoots)
            .Select(CanonicalFilePath.Normalize)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (StartupProjectStore.TryLoad(solutionPath, out var rel) && !string.IsNullOrEmpty(rel))
        {
            var full = CanonicalFilePath.Normalize(Path.Combine(solutionDirectory, rel));
            if (LaunchProjectPathResolver.NormalizeExistingProjectFileFullPath(full) is not null
                && projects.Contains(full))
                return new Result(full, Path.GetFileNameWithoutExtension(full));

            StartupProjectStore.Clear(solutionPath);
        }

        return TryDefaultSingleManagedProject(solutionPath, solutionDirectory, solutionRoots, projects);
    }

    private static Result TryDefaultSingleManagedProject(
        string solutionPath,
        string solutionDirectory,
        System.Collections.ObjectModel.ObservableCollection<SolutionItem> solutionRoots,
        HashSet<string> projectPathSet)
    {
        var csprojs = McpSolutionTree.CollectDistinctManagedProjectPaths(solutionRoots);
        if (csprojs.Count != 1)
            return Empty;

        var only = csprojs[0];
        if (!projectPathSet.Contains(only))
            return Empty;

        if (!LaunchProjectRelativePath.TryGetRelativeToSolutionDirectory(solutionDirectory, only, out var rel, out _))
            return Empty;

        try
        {
            StartupProjectStore.Save(solutionPath, rel);
        }
        catch
        {
            // сохранение опционально — в памяти стартовый проект всё равно будет
        }

        return new Result(only, Path.GetFileNameWithoutExtension(only));
    }
}
