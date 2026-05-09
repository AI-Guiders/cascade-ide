#nullable enable
using System.Collections.ObjectModel;
using System.Collections.Generic;
using CascadeIDE.Contracts;
using CascadeIDE.Models;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// Обход дерева <see cref="SolutionItem"/> для MCP: проекты, файлы, относительные пути, дерево для JSON.
/// Без привязки к UI.
/// </summary>
[ComputingUnit]
public static class McpSolutionTree
{
    public static IEnumerable<string> CollectProjectPaths(ObservableCollection<SolutionItem> roots)
    {
        foreach (var item in roots)
        {
            if (item.FullPath is { } p && (p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)))
                yield return p;
            foreach (var child in CollectProjectPaths(item.Children))
                yield return child;
        }
    }

    /// <summary>Нормализованные пути <c>.csproj</c>/<c>.fsproj</c> из дерева (без .sln).</summary>
    public static List<string> CollectDistinctManagedProjectPaths(ObservableCollection<SolutionItem> roots) =>
        CollectProjectPaths(roots)
            .Where(static p => p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) ||
                               p.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
            .Select(static p => CanonicalFilePath.Normalize(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IEnumerable<(string Title, string FullPath)> CollectFileEntries(ObservableCollection<SolutionItem> roots)
    {
        foreach (var item in roots)
        {
            if (item.FullPath is { } p && !p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) && !p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                yield return (item.Title, p);
            foreach (var child in CollectFileEntries(item.Children))
                yield return child;
        }
    }

    /// <summary>
    /// Полный путь файла → путь владеющего <c>.csproj</c> (или <c>null</c>, если файл вне проекта в дереве).
    /// Обход совпадает с обозревателем решения: узел с <c>.csproj</c> задаёт контекст для потомков.
    /// Для «реального» MSBuild-проекта файла при плоском SDK-glob см. <see cref="ResolveOwningProjectPath"/>.
    /// </summary>
    public static Dictionary<string, string?> MapFileToProject(ObservableCollection<SolutionItem> roots)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        void Walk(SolutionItem node, string? projectPath)
        {
            if (node.FullPath is { } p)
            {
                if (p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    string proj;
                    try
                    {
                        proj = CanonicalFilePath.Normalize(p);
                    }
                    catch
                    {
                        return;
                    }

                    foreach (var child in node.Children)
                        Walk(child, proj);
                    return;
                }

                if (!p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase)
                    && !p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Первое вхождение пути в DFS определяет проект; не перезаписывать —
                        // иначе последний узел в дереве «перетягивает» общие/линкованные пути на чужой .csproj.
                        map.TryAdd(CanonicalFilePath.Normalize(p), projectPath);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            if (node.FullPath is { } fp && fp.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                return;

            foreach (var child in node.Children)
                Walk(child, projectPath);
        }

        foreach (var root in roots)
            Walk(root, null);

        return map;
    }

    /// <summary>
    /// Ближайший вверх по диску <c>.csproj</c>, которому по соглашению принадлежит файл (папка проекта = каталог с .csproj).
    /// Нужен для <c>project_peer</c>: узел дерева решения может подвешивать все <c>.cs</c> под одним корневым .csproj из-за SDK-glob по каталогу.
    /// </summary>
    public static string? ResolveOwningProjectPath(string fileFullPath)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath))
            return null;
        string full;
        try
        {
            full = CanonicalFilePath.Normalize(fileFullPath.Trim());
        }
        catch
        {
            return null;
        }

        var dir = Path.GetDirectoryName(full);
        while (!string.IsNullOrEmpty(dir))
        {
            string[] csprojs;
            try
            {
                csprojs = Directory.GetFiles(dir, "*.csproj");
            }
            catch
            {
                break;
            }

            if (csprojs.Length > 0)
            {
                if (csprojs.Length == 1)
                    return CanonicalFilePath.Normalize(csprojs[0]);
                var folderName = Path.GetFileName(dir);
                var match = csprojs.FirstOrDefault(p =>
                    string.Equals(Path.GetFileNameWithoutExtension(p), folderName, StringComparison.OrdinalIgnoreCase));
                return CanonicalFilePath.Normalize(match ?? csprojs[0]);
            }

            try
            {
                dir = Path.GetDirectoryName(dir);
            }
            catch
            {
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Артефакты сборки (obj/bin) — не участвуют в семантической навигации и часто попадают в дерево из-за явных Include в .csproj.
    /// </summary>
    public static bool IsBuildArtifactPath(string fullPath)
    {
        if (string.IsNullOrEmpty(fullPath))
            return false;
        return fullPath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || fullPath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || fullPath.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
            || fullPath.Contains("/bin/", StringComparison.OrdinalIgnoreCase);
    }

    public static string? GetRelativePath(string? solutionPath, string? fullPath)
    {
        if (string.IsNullOrEmpty(solutionPath) || string.IsNullOrEmpty(fullPath))
            return null;
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
            return null;
        try
        {
            return Path.GetRelativePath(solutionDir, fullPath);
        }
        catch
        {
            return null;
        }
    }

    public static object BuildSolutionTreeNode(SolutionItem item, string? solutionPath)
    {
        var relative = GetRelativePath(solutionPath, item.FullPath);
        var path = item.FullPath;
        var title = item.Title;
        if (item.Children.Count == 0)
            return new { title, path, relative_path = relative };
        var children = item.Children.Select(c => BuildSolutionTreeNode(c, solutionPath)).ToList();
        return new { title, path, relative_path = relative, children };
    }
}
