#nullable enable
using System.Collections.ObjectModel;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>Раскрытие узлов SE: дефолт после load и путь к активному файлу (ADR 0167).</summary>
public static class SolutionTreeExpansionPolicy
{
    public static void ApplyDefaultExpansion(IEnumerable<SolutionItem> roots)
    {
        foreach (var root in roots)
            ApplyDefaultExpansionRecursive(root);
    }

    public static bool TryExpandPathTo(ObservableCollection<SolutionItem> roots, SolutionItem target)
    {
        if (!TryFindPath(roots, target, out var path))
            return false;

        foreach (var node in path)
            node.IsExpanded = true;

        return true;
    }

    private static void ApplyDefaultExpansionRecursive(SolutionItem node)
    {
        node.IsExpanded = ShouldExpandByDefault(node);
        foreach (var child in node.Children)
            ApplyDefaultExpansionRecursive(child);
    }

    private static bool ShouldExpandByDefault(SolutionItem node)
    {
        if (node.FullPath is { } path)
        {
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool TryFindPath(
        IEnumerable<SolutionItem> nodes,
        SolutionItem target,
        out List<SolutionItem> path)
    {
        foreach (var node in nodes)
        {
            if (ReferenceEquals(node, target))
            {
                path = [node];
                return true;
            }

            if (TryFindPath(node.Children, target, out var childPath))
            {
                childPath.Insert(0, node);
                path = childPath;
                return true;
            }
        }

        path = [];
        return false;
    }
}
