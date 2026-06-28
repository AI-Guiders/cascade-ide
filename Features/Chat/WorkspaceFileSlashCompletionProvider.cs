#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Chat;

/// <summary>Кэш путей из дерева решения; фильтр по префиксу (ADR 0125) через <see cref="WorkspaceFileIndex"/>.</summary>
public sealed class WorkspaceFileSlashCompletionProvider : IWorkspaceFileSlashCompletionProvider
{
    private readonly WorkspaceFileIndex _index = new();
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

        _index.Invalidate(roots, _getSolutionPath(), _getWorkspaceRoot());
        return _index.Search(pathPrefix, limit)
            .Select(m => new WorkspaceFileSlashMatch(m.InsertPath, m.Help))
            .ToList();
    }
}
