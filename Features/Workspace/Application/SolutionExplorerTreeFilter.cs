#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// Фильтр SE: пересборка видимого дерева (Avalonia TreeView не умеет prune через IsVisible).
/// </summary>
public static class SolutionExplorerTreeFilter
{
    public static void RebuildDisplayRoots(
        ObservableCollection<SolutionItem> sourceRoots,
        ObservableCollection<SolutionItem> displayRoots,
        string filterText,
        WorkspaceFileIndex index)
    {
        displayRoots.Clear();
        var term = filterText.Trim();
        if (term.Length == 0)
        {
            foreach (var root in sourceRoots)
                displayRoots.Add(root);
            return;
        }

        var matches = index.MatchingFullPaths(term);
        foreach (var root in sourceRoots)
        {
            var filtered = FilterNode(root, term, matches);
            if (filtered is not null)
                displayRoots.Add(filtered);
        }
    }

    internal static SolutionItem? FilterNode(SolutionItem node, string term, HashSet<string> matchingPaths)
    {
        var selfMatches = NodeMatches(node, matchingPaths, term);
        var filteredChildren = new List<SolutionItem>();
        foreach (var child in node.Children)
        {
            var filteredChild = FilterNode(child, term, matchingPaths);
            if (filteredChild is not null)
                filteredChildren.Add(filteredChild);
        }

        if (!selfMatches && filteredChildren.Count == 0)
            return null;

        var result = CloneForFilter(node);
        foreach (var child in filteredChildren)
            result.Children.Add(child);
        return result;
    }

    private static SolutionItem CloneForFilter(SolutionItem source)
    {
        SolutionItem clone = source.FullPath switch
        {
            null => SolutionItem.CreateFolder(source.Title),
            { } path when source.IconKey.Equals("solution", StringComparison.OrdinalIgnoreCase)
                => SolutionItem.CreateSolution(source.Title, path),
            { } path when source.IconKey.Equals("project", StringComparison.OrdinalIgnoreCase)
                => SolutionItem.CreateProject(source.Title, path),
            { } path when Directory.Exists(path)
                => SolutionItem.CreateFolderWorkspaceRoot(source.Title, path),
            { } path => SolutionItem.CreateFile(source.Title, path),
        };
        clone.IsExpanded = true;
        return clone;
    }

    private static bool NodeMatches(SolutionItem node, HashSet<string> matchingPaths, string term)
    {
        if (node.FullPath is { } path)
        {
            if (matchingPaths.Contains(path))
                return true;

            return node.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
                || path.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        return node.Title.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}
