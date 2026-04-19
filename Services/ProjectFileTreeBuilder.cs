using System.Xml.Linq;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Дерево файлов проекта для обозревателя: разбор .csproj (Compile/None/…), SDK-glob,
/// <see cref="DependentUpon"/> и эвристика partial-классов. Не занимается загрузкой .sln.
/// </summary>
public static class ProjectFileTreeBuilder
{
    /// <param name="solutionBaseDirectoryForIgnore">Каталог с <c>.sln</c> — для поиска корня репозитория и загрузки <c>.gitignore</c> / <c>.cascadeignore</c>.</param>
    public static void AddProjectFileChildren(SolutionItem projectNode, string projectPath, string? solutionBaseDirectoryForIgnore = null)
    {
        if (!File.Exists(projectPath))
            return;

        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        var hintForRepo = string.IsNullOrWhiteSpace(solutionBaseDirectoryForIgnore)
            ? projectDir
            : solutionBaseDirectoryForIgnore.Trim();
        var repoRoot = WorkspaceIgnoreMatcher.ResolveRepositoryRoot(hintForRepo);
        var ignore = WorkspaceIgnoreMatcher.GetOrCreate(repoRoot);
        var fileEntries = new List<(string RelativePath, string FullPath)>();

        try
        {
            using var stream = File.OpenRead(projectPath);
            var doc = XDocument.Load(stream);

            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var explicitNesting = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var itemGroup in doc.Descendants().Where(e => e.Name.LocalName == "ItemGroup"))
            {
                foreach (var item in itemGroup.Elements())
                {
                    var localName = item.Name.LocalName;
                    if (localName != "Compile" && localName != "None" && localName != "Page" && localName != "AvaloniaResource")
                        continue;

                    var include = (string?)item.Attribute("Include") ?? (string?)item.Attribute("Update");
                    if (string.IsNullOrWhiteSpace(include))
                        continue;

                    var ext = Path.GetExtension(include);
                    if (!string.Equals(ext, ".cs", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ext, ".axaml", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ext, ".xaml", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ext, ".md", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(ext, ".markdown", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var normalizedInclude = include.Replace('\\', Path.DirectorySeparatorChar);
                    var fullPath = Path.GetFullPath(Path.Combine(projectDir, normalizedInclude));
                    if (ignore.IsIgnored(fullPath))
                        continue;
                    if (item.Attribute("Include") is not null &&
                        included.Add(fullPath) && File.Exists(fullPath))
                        fileEntries.Add((normalizedInclude, fullPath));

                    var depEl = item.Elements().FirstOrDefault(e => e.Name.LocalName == "DependentUpon");
                    var depRaw = depEl?.Value.Trim();
                    if (string.IsNullOrEmpty(depRaw) || !File.Exists(fullPath))
                        continue;
                    var parentFull = ResolveDependentUponTarget(projectDir, normalizedInclude, depRaw);
                    if (parentFull is not null && File.Exists(parentFull) &&
                        !string.Equals(fullPath, parentFull, StringComparison.OrdinalIgnoreCase))
                        explicitNesting[fullPath] = parentFull;
                }
            }

            // SDK-стиль: в XML часто нет явных Compile — сканируем каталог
            if (fileEntries.Count == 0)
            {
                foreach (var f in Directory.EnumerateFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(projectDir, "*.axaml", SearchOption.AllDirectories))
                    .Concat(Directory.EnumerateFiles(projectDir, "*.xaml", SearchOption.AllDirectories))
                    .Concat(Directory.EnumerateFiles(projectDir, "*.md", SearchOption.AllDirectories))
                    .Concat(Directory.EnumerateFiles(projectDir, "*.markdown", SearchOption.AllDirectories)))
                {
                    var rel = Path.GetRelativePath(projectDir, f);
                    if (rel.StartsWith("obj", StringComparison.OrdinalIgnoreCase) ||
                        rel.StartsWith("bin", StringComparison.OrdinalIgnoreCase))
                        continue;
                    var fp = Path.GetFullPath(f);
                    if (ignore.IsIgnored(fp))
                        continue;
                    if (!included.Add(fp))
                        continue;
                    fileEntries.Add((rel, fp));
                }
            }

            var nestingPairs = MergeNestingPairs(fileEntries, explicitNesting);
            AddFileEntriesAsTree(projectNode, fileEntries);
            ApplyDependentNesting(projectNode, nestingPairs);
        }
        catch
        {
            // Оставляем проект без дочерних файлов
        }
    }

    /// <summary>Сортировка узлов дерева решения/проекта: папки, затем файлы по имени (рекурсивно).</summary>
    public static void SortSolutionItemChildren(SolutionItem node, StringComparer comparer)
    {
        var list = node.Children;
        if (list.Count == 0)
            return;
        var ordered = list
            .OrderBy(c => c.FullPath is not null ? 1 : 0)
            .ThenBy(c => c.Title, comparer)
            .ToList();
        list.Clear();
        foreach (var c in ordered)
        {
            list.Add(c);
            SortSolutionItemChildren(c, comparer);
        }
    }

    /// <summary>
    /// Целевой файл родителя для <see cref="DependentUpon"/> в .csproj (как в Visual Studio).
    /// </summary>
    private static string? ResolveDependentUponTarget(string projectDir, string childProjectRelativePath, string dependentUpon)
    {
        dependentUpon = dependentUpon.Replace('/', Path.DirectorySeparatorChar).Trim();
        if (dependentUpon.Length == 0)
            return null;

        if (dependentUpon.Contains(Path.DirectorySeparatorChar))
            return Path.GetFullPath(Path.Combine(projectDir, dependentUpon));

        var childDir = Path.GetDirectoryName(childProjectRelativePath);
        if (string.IsNullOrEmpty(childDir))
            return Path.GetFullPath(Path.Combine(projectDir, dependentUpon));
        return Path.GetFullPath(Path.Combine(projectDir, childDir, dependentUpon));
    }

    /// <summary>
    /// Для SDK-glob: вложить <c>Stem.Rest.cs</c> под самый длинный существующий <c>Stem.cs</c> в той же папке (partial-классы).
    /// Явные пары из .csproj имеют приоритет.
    /// </summary>
    private static List<(string ChildFullPath, string ParentFullPath)> MergeNestingPairs(
        List<(string RelativePath, string FullPath)> fileEntries,
        Dictionary<string, string> explicitNesting)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var pairs = new List<(string, string)>();
        foreach (var kv in explicitNesting)
            pairs.Add((kv.Key, kv.Value));

        var explicitChild = new HashSet<string>(explicitNesting.Keys, comparer);
        var byDir = fileEntries.GroupBy(e => Path.GetDirectoryName(e.FullPath) ?? "", comparer);
        foreach (var group in byDir)
        {
            var names = new HashSet<string>(
                group.Select(g => Path.GetFileName(g.FullPath)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var (_, full) in group)
            {
                if (explicitChild.Contains(full))
                    continue;
                if (!full.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;
                var parentName = TryInferParentCsFileName(Path.GetFileName(full), names);
                if (parentName is null)
                    continue;
                var parentFull = Path.GetFullPath(Path.Combine(group.Key, parentName));
                if (!File.Exists(parentFull) || comparer.Equals(full, parentFull))
                    continue;
                pairs.Add((full, parentFull));
            }
        }

        return pairs;
    }

    private static string? TryInferParentCsFileName(string fileName, HashSet<string> fileNamesInSameDir)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(stem))
            return null;
        var parts = stem.Split('.');
        if (parts.Length < 2)
            return null;
        for (var i = parts.Length - 1; i >= 1; i--)
        {
            var candidate = string.Join(".", parts.Take(i)) + ".cs";
            if (fileNamesInSameDir.Contains(candidate))
                return candidate;
        }
        return null;
    }

    private static void ApplyDependentNesting(SolutionItem projectNode, List<(string ChildFullPath, string ParentFullPath)> pairs)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var seenChild = new HashSet<string>(comparer);
        foreach (var (childPath, parentPath) in pairs
                     .OrderByDescending(p => p.ChildFullPath.Length))
        {
            if (!seenChild.Add(childPath))
                continue;
            if (comparer.Equals(childPath, parentPath))
                continue;
            if (!TryRemoveFileNodeByFullPath(projectNode, childPath, out var childNode) || childNode is null)
                continue;
            var parentNode = FindFileNodeByFullPath(projectNode, parentPath);
            if (parentNode is null)
                continue;
            if (IsDescendantOf(childNode, parentPath, comparer))
                continue;
            parentNode.Children.Add(childNode);
        }

        SortSolutionItemChildren(projectNode, comparer);
    }

    private static bool IsDescendantOf(SolutionItem node, string ancestorFullPath, StringComparer comparer)
    {
        foreach (var c in node.Children)
        {
            if (c.FullPath is not null && comparer.Equals(c.FullPath, ancestorFullPath))
                return true;
            if (IsDescendantOf(c, ancestorFullPath, comparer))
                return true;
        }
        return false;
    }

    private static bool TryRemoveFileNodeByFullPath(SolutionItem root, string fullPath, out SolutionItem? removed)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        removed = null;
        for (var i = 0; i < root.Children.Count; i++)
        {
            var c = root.Children[i];
            if (c.FullPath is not null && comparer.Equals(c.FullPath, fullPath))
            {
                removed = c;
                root.Children.RemoveAt(i);
                return true;
            }

            if (TryRemoveFileNodeByFullPath(c, fullPath, out removed))
                return true;
        }

        return false;
    }

    private static SolutionItem? FindFileNodeByFullPath(SolutionItem root, string fullPath)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        if (root.FullPath is not null && comparer.Equals(root.FullPath, fullPath))
            return root;
        foreach (var c in root.Children)
        {
            var found = FindFileNodeByFullPath(c, fullPath);
            if (found is not null)
                return found;
        }

        return null;
    }

    /// <summary>Добавляет файлы в дерево проекта: папки по относительному пути, единый порядок (папки, затем файлы по имени).</summary>
    private static void AddFileEntriesAsTree(SolutionItem projectNode, List<(string RelativePath, string FullPath)> fileEntries)
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        foreach (var (relativePath, fullPath) in fileEntries)
        {
            var parts = relativePath.Split(Path.DirectorySeparatorChar, '/').Where(p => p.Length > 0).ToList();
            if (parts.Count == 0)
                continue;
            if (parts.Count == 1)
            {
                projectNode.Children.Add(SolutionItem.CreateFile(parts[0], fullPath));
                continue;
            }
            SolutionItem current = projectNode;
            for (var i = 0; i < parts.Count - 1; i++)
            {
                var segment = parts[i];
                var folder = current.Children.FirstOrDefault(c => c.FullPath is null && comparer.Equals(c.Title, segment));
                if (folder is null)
                {
                    folder = SolutionItem.CreateFolder(segment);
                    current.Children.Add(folder);
                }
                current = folder;
            }
            current.Children.Add(SolutionItem.CreateFile(parts[^1], fullPath));
        }
        SortSolutionItemChildren(projectNode, comparer);
    }
}
