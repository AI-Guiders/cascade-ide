using CascadeIDE.Models;

namespace CascadeIDE.Features.Documents;

/// <summary>
/// Нормализация путей и поиск узла в дереве решения без исключений на кривых <see cref="SolutionItem.FullPath"/>.
/// </summary>
internal static class SolutionTreePath
{
    /// <summary><see cref="Path.GetFullPath"/> без исключений на невалидных строках (дерево, MCP, карта намерений).</summary>
    public static bool TryGetFullPath(string path, out string fullPath)
    {
        fullPath = "";
        try
        {
            fullPath = Path.GetFullPath(path);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>Обход дерева: узлы с путями, которые нельзя нормализовать, пропускаются (не бросают).</summary>
    public static SolutionItem? FindItemByFullPath(IEnumerable<SolutionItem> items, string normalizedFullPath)
    {
        foreach (var node in items)
        {
            if (node.FullPath is not null
                && TryGetFullPath(node.FullPath, out var nodeFull)
                && string.Equals(nodeFull, normalizedFullPath, StringComparison.OrdinalIgnoreCase))
                return node;
            var found = FindItemByFullPath(node.Children, normalizedFullPath);
            if (found is not null)
                return found;
        }

        return null;
    }
}
