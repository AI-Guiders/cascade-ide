#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Единый индекс путей solution/workspace (ADR 0167): slash, Go to File, фильтр SE.</summary>
public sealed class WorkspaceFileIndex
{
    private readonly List<WorkspaceFileEntry> _entries = [];
    private string? _solutionPath;
    private string _workspaceRoot = "";

    public void Invalidate(
        ObservableCollection<SolutionItem> roots,
        string? solutionPath,
        string workspaceRoot)
    {
        _solutionPath = solutionPath;
        _workspaceRoot = workspaceRoot.Trim();
        _entries.Clear();

        if (roots.Count == 0)
            return;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (title, fullPath) in McpSolutionTree.CollectFileEntries(roots))
        {
            if (!TryToInsertPath(fullPath, out var insert))
                continue;
            if (!seen.Add(insert))
                continue;
            _entries.Add(new WorkspaceFileEntry(title, fullPath, insert, insert));
        }

        foreach (var projectPath in McpSolutionTree.CollectProjectPaths(roots))
        {
            if (!TryToInsertPath(projectPath, out var insert))
                continue;
            if (!seen.Add(insert))
                continue;
            var name = Path.GetFileName(projectPath);
            _entries.Add(new WorkspaceFileEntry(name, projectPath, insert, name));
        }
    }

    public IReadOnlyList<WorkspaceFileMatch> Search(string query, int limit)
    {
        if (limit <= 0)
            return [];

        var prefix = NormalizeQuery(query);
        IEnumerable<(WorkspaceFileEntry Entry, int Rank)> ranked = _entries
            .Select(e => (e, Rank(prefix, e.InsertPath, e.Title, e.FullPath)));

        if (prefix.Length > 0)
            ranked = ranked.Where(x => x.Rank < int.MaxValue);

        return ranked
            .OrderBy(x => x.Rank)
            .ThenBy(x => x.Entry.InsertPath, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .Select(x => new WorkspaceFileMatch(
                x.Entry.Title,
                x.Entry.FullPath,
                x.Entry.InsertPath,
                x.Entry.Help,
                x.Rank))
            .ToList();
    }

    public HashSet<string> MatchingFullPaths(string query)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in Search(query, int.MaxValue))
            set.Add(m.FullPath);
        return set;
    }

    private bool TryToInsertPath(string fullPath, out string insertPath)
    {
        insertPath = "";
        var relative = McpSolutionTree.GetRelativePath(_solutionPath, fullPath);
        if (!string.IsNullOrWhiteSpace(relative))
        {
            insertPath = relative.Replace('\\', '/');
            return true;
        }

        if (string.IsNullOrWhiteSpace(_workspaceRoot))
            return false;

        try
        {
            var rootFull = Path.GetFullPath(_workspaceRoot);
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

    internal static string NormalizeQuery(string query) =>
        query.Trim().Replace('\\', '/');

    internal static int Rank(string prefix, string insertPath, string title, string fullPath)
    {
        if (prefix.Length == 0)
            return 0;

        if (insertPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return 0;

        if (insertPath.Contains(prefix, StringComparison.OrdinalIgnoreCase)
            || title.Contains(prefix, StringComparison.OrdinalIgnoreCase)
            || fullPath.Contains(prefix, StringComparison.OrdinalIgnoreCase))
            return 1;

        return int.MaxValue;
    }

    private sealed record WorkspaceFileEntry(string Title, string FullPath, string InsertPath, string Help);
}

public readonly record struct WorkspaceFileMatch(
    string Title,
    string FullPath,
    string InsertPath,
    string Help,
    int Rank);
