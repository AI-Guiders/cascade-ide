#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Chat;

/// <summary>Кэш путей из дерева решения; фильтр по префиксу (ADR 0125).</summary>
public sealed class WorkspaceFileSlashCompletionProvider : IWorkspaceFileSlashCompletionProvider
{
    private readonly Func<string?> _getSolutionPath;
    private readonly Func<ObservableCollection<SolutionItem>> _getSolutionRoots;
    private readonly Func<string> _getWorkspaceRoot;

    public WorkspaceFileSlashCompletionProvider(
        Func<string?> getSolutionPath,
        Func<ObservableCollection<SolutionItem>> getSolutionRoots,
        Func<string> getWorkspaceRoot)
    {
        _getSolutionPath = getSolutionPath;
        _getSolutionRoots = getSolutionRoots;
        _getWorkspaceRoot = getWorkspaceRoot;
    }

    public IReadOnlyList<WorkspaceFileSlashMatch> GetMatches(string pathPrefix, int limit)
    {
        if (limit <= 0)
            return [];

        var roots = _getSolutionRoots();
        if (roots.Count == 0)
            return [];

        var solutionPath = _getSolutionPath();
        var workspaceRoot = _getWorkspaceRoot().Trim();
        var prefix = NormalizePrefix(pathPrefix);
        var entries = BuildEntries(roots, solutionPath, workspaceRoot);
        if (entries.Count == 0)
            return [];

        IEnumerable<(string InsertPath, string Help, int Rank)> ranked = entries
            .Select(e => (e.InsertPath, e.Help, Rank(prefix, e.InsertPath)));

        if (prefix.Length > 0)
            ranked = ranked.Where(e => e.Rank < int.MaxValue);

        return ranked
            .OrderBy(e => e.Rank)
            .ThenBy(e => e.InsertPath, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(e => new WorkspaceFileSlashMatch(e.InsertPath, e.Help))
            .ToList();
    }

    private List<(string InsertPath, string Help)> BuildEntries(
        ObservableCollection<SolutionItem> roots,
        string? solutionPath,
        string workspaceRoot)
    {
        var list = new List<(string InsertPath, string Help)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (title, fullPath) in McpSolutionTree.CollectFileEntries(roots))
        {
            if (!TryToInsertPath(fullPath, solutionPath, workspaceRoot, out var insert))
                continue;

            if (!seen.Add(insert))
                continue;

            list.Add((insert, insert));
        }

        foreach (var projectPath in McpSolutionTree.CollectProjectPaths(roots))
        {
            if (!TryToInsertPath(projectPath, solutionPath, workspaceRoot, out var insert))
                continue;

            if (!seen.Add(insert))
                continue;

            var name = Path.GetFileName(projectPath);
            list.Add((insert, name));
        }

        return list;
    }

    private static bool TryToInsertPath(
        string fullPath,
        string? solutionPath,
        string workspaceRoot,
        out string insertPath)
    {
        insertPath = "";
        var relative = McpSolutionTree.GetRelativePath(solutionPath, fullPath);
        if (!string.IsNullOrWhiteSpace(relative))
        {
            insertPath = relative.Replace('\\', '/');
            return true;
        }

        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return false;

        try
        {
            var rootFull = Path.GetFullPath(workspaceRoot);
            var fileFull = Path.GetFullPath(fullPath);
            if (!fileFull.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                return false;

            insertPath = Path.GetRelativePath(rootFull, fileFull).Replace('\\', '/');
            return insertPath.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePrefix(string pathPrefix) =>
        pathPrefix.Trim().Replace('\\', '/');

    private static int Rank(string prefix, string insertPath)
    {
        if (prefix.Length == 0)
            return 0;

        if (insertPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (insertPath.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            return 1;

        return int.MaxValue;
    }
}
