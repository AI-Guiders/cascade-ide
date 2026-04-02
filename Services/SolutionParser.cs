using System.Xml.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CascadeIDE.Models;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace CascadeIDE.Services;

public static class SolutionParser
{
    public static SolutionItem? Load(string solutionPath, out string? error)
    {
        error = null;
        solutionPath = solutionPath?.Trim() ?? "";
        if (solutionPath.Length == 0)
        {
            error = "Путь пустой.";
            return null;
        }

        if (Uri.TryCreate(solutionPath, UriKind.Absolute, out var uri) && uri.IsFile)
            solutionPath = uri.LocalPath;

        string normalized;
        try
        {
            normalized = Path.GetFullPath(solutionPath);
        }
        catch (Exception ex)
        {
            error = "Путь: " + ex.Message;
            return null;
        }

        if (!File.Exists(normalized))
        {
            error = "Файл не найден: " + normalized;
            return null;
        }

        var dir = Path.GetDirectoryName(normalized) ?? "";
        var name = Path.GetFileNameWithoutExtension(normalized);
        var ext = Path.GetExtension(normalized);

        if (string.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".sln", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".slnf", StringComparison.OrdinalIgnoreCase))
        {
            var root = TryLoadWithSolutionPersistence(normalized, dir, name, out var err);
            if (root is not null)
                return root;
            // Fall back to legacy parsers for resilience (partial formats, edge cases).
            error = err;
            if (string.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase))
                return LoadSlnx(normalized, dir, name);
            if (string.Equals(ext, ".slnf", StringComparison.OrdinalIgnoreCase))
                return LoadSlnf(normalized, dir, name, out error);
            return LoadSln(normalized, dir, name);
        }

        error = "Расширение не .slnx, не .sln и не .slnf.";
        return null;
    }

    private static SolutionItem? TryLoadWithSolutionPersistence(
        string solutionPath,
        string baseDir,
        string solutionName,
        out string? error)
    {
        error = null;
        try
        {
            var serializer = SolutionSerializers.GetSerializerByMoniker(solutionPath);
            if (serializer is null)
            {
                error = "Не удалось подобрать сериализатор решения для файла.";
                return null;
            }

            // OpenAsync is async; keep SolutionParser API sync for now.
            SolutionModel model = serializer.OpenAsync(solutionPath, CancellationToken.None).GetAwaiter().GetResult();

            var root = SolutionItem.CreateSolution(solutionName, solutionPath);
            // Try to restore solution folder hierarchy when available.
            // The public model surface has evolved; use minimal reflection to map parent folders.
            var folderCache = new Dictionary<object, SolutionItem>(ReferenceEqualityComparer.Instance);

            SolutionItem GetOrCreateFolderNode(object folderObj)
            {
                if (folderCache.TryGetValue(folderObj, out var existing))
                    return existing;

                var folderName = TryGetStringProperty(folderObj, "Name", "FolderName", "Path") ?? "Folder";
                var folderNode = EnsureSolutionFolders(root, folderName);
                folderCache[folderObj] = folderNode;
                return folderNode;
            }

            foreach (var project in model.SolutionProjects)
            {
                var projectPath = project.FilePath;
                if (string.IsNullOrWhiteSpace(projectPath))
                    continue;

                var fullPath = Path.GetFullPath(Path.Combine(baseDir, projectPath.Replace('/', Path.DirectorySeparatorChar)));
                var title = Path.GetFileName(projectPath);
                var projectNode = SolutionItem.CreateProject(title, fullPath);
                AddProjectFileChildren(projectNode, fullPath);

                SolutionItem parent = root;
                try
                {
                    var parentObj =
                        TryGetProperty(project, "Parent") ??
                        TryGetProperty(project, "ParentFolder") ??
                        TryGetProperty(project, "SolutionFolder");

                    if (parentObj is not null)
                        parent = GetOrCreateFolderNode(parentObj);
                }
                catch
                {
                    parent = root;
                }

                parent.Children.Add(projectNode);
            }

            SortSolutionItemChildren(root, StringComparer.OrdinalIgnoreCase);
            return root;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private static SolutionItem LoadSlnx(string solutionPath, string baseDir, string solutionName)
    {
        var root = SolutionItem.CreateSolution(solutionName, solutionPath);
        using var stream = File.OpenRead(solutionPath);
        var doc = XDocument.Load(stream);

        var solutionEl = doc.Root;
        if (solutionEl is null)
            return root;

        void VisitContainer(XElement container, SolutionItem parentFolder)
        {
            foreach (var child in container.Elements())
            {
                if (child.Name.LocalName == "Folder")
                {
                    var folderName = ((string?)child.Attribute("Name"))?.Trim() ?? "";
                    var folderNode = EnsureSolutionFolders(parentFolder, folderName);
                    VisitContainer(child, folderNode);
                    continue;
                }

                if (child.Name.LocalName != "Project")
                    continue;

                var path = (string?)child.Attribute("Path");
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var fullPath = Path.GetFullPath(Path.Combine(baseDir, path.Replace('/', Path.DirectorySeparatorChar)));
                var title = Path.GetFileName(path);
                var projectNode = SolutionItem.CreateProject(title, fullPath);
                AddProjectFileChildren(projectNode, fullPath);
                parentFolder.Children.Add(projectNode);
            }
        }

        VisitContainer(solutionEl, root);
        SortSolutionItemChildren(root, StringComparer.OrdinalIgnoreCase);

        return root;
    }

    private static SolutionItem LoadSln(string solutionPath, string baseDir, string solutionName)
    {
        var root = SolutionItem.CreateSolution(solutionName, solutionPath);
        var text = File.ReadAllText(solutionPath);

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("Project(", StringComparison.Ordinal))
                continue;

            var path = ExtractPathFromSlnProjectLine(trimmed);
            if (string.IsNullOrEmpty(path) || path.Contains("*.vcxproj") || path.Contains(".vcxproj"))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(baseDir, path.Replace('\\', Path.DirectorySeparatorChar)));
            var title = Path.GetFileName(path);
            var projectNode = SolutionItem.CreateProject(title, fullPath);
            AddProjectFileChildren(projectNode, fullPath);
            root.Children.Add(projectNode);
        }

        return root;
    }

    private static SolutionItem? LoadSlnf(string slnfPath, string baseDir, string filterName, out string? error)
    {
        error = null;
        try
        {
            using var stream = File.OpenRead(slnfPath);
            var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("solution", out var solutionEl))
            {
                error = "Некорректный .slnf: нет поля solution.";
                return null;
            }

            string baseSolutionRel = "";
            if (solutionEl.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
                baseSolutionRel = pathEl.GetString() ?? "";

            if (string.IsNullOrWhiteSpace(baseSolutionRel))
            {
                error = "Некорректный .slnf: solution.path пуст.";
                return null;
            }

            var baseSolutionPath = Path.GetFullPath(Path.Combine(baseDir, baseSolutionRel.Replace('/', Path.DirectorySeparatorChar)));
            if (!File.Exists(baseSolutionPath))
            {
                error = "Базовый solution не найден: " + baseSolutionPath;
                return null;
            }

            var allowedProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (solutionEl.TryGetProperty("projects", out var projectsEl) && projectsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var p in projectsEl.EnumerateArray())
                {
                    if (p.ValueKind != JsonValueKind.String)
                        continue;
                    var rel = (p.GetString() ?? "").Trim();
                    if (rel.Length == 0)
                        continue;
                    var full = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(baseSolutionPath) ?? baseDir, rel.Replace('/', Path.DirectorySeparatorChar)));
                    allowedProjects.Add(full);
                }
            }

            // Parse base solution and then filter its project set.
            var baseDir2 = Path.GetDirectoryName(baseSolutionPath) ?? baseDir;
            var solutionName = Path.GetFileNameWithoutExtension(baseSolutionPath);
            var ext = Path.GetExtension(baseSolutionPath);

            SolutionItem? baseRoot = null;
            string? innerErr = null;
            if (string.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(ext, ".sln", StringComparison.OrdinalIgnoreCase))
            {
                baseRoot = TryLoadWithSolutionPersistence(baseSolutionPath, baseDir2, solutionName, out innerErr)
                           ?? (string.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase)
                               ? LoadSlnx(baseSolutionPath, baseDir2, solutionName)
                               : LoadSln(baseSolutionPath, baseDir2, solutionName));
            }
            else
            {
                // Unexpected base solution type; still try legacy sln parsing if it looks like it.
                baseRoot = LoadSln(baseSolutionPath, baseDir2, solutionName);
            }

            if (baseRoot is null)
            {
                error = innerErr ?? "Не удалось загрузить базовый solution для .slnf.";
                return null;
            }

            if (allowedProjects.Count == 0)
            {
                // If projects list is empty/missing, slnf is effectively "load base solution".
                return baseRoot;
            }

            PruneTreeToAllowedProjects(baseRoot, allowedProjects);
            SortSolutionItemChildren(baseRoot, StringComparer.OrdinalIgnoreCase);
            return baseRoot;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private static bool PruneTreeToAllowedProjects(SolutionItem node, HashSet<string> allowedProjects)
    {
        // Returns whether this subtree contains any allowed project.
        if (node.FullPath is { } fp && fp.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            try { fp = Path.GetFullPath(fp); } catch { /* ignore */ }
            return allowedProjects.Contains(fp);
        }

        for (var i = node.Children.Count - 1; i >= 0; i--)
        {
            var child = node.Children[i];
            var keep = PruneTreeToAllowedProjects(child, allowedProjects);
            if (!keep)
                node.Children.RemoveAt(i);
        }

        // Keep folders/solution root if they still have children.
        return node.Children.Count > 0 || node.FullPath is not null && node.FullPath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddProjectFileChildren(SolutionItem projectNode, string projectPath)
    {
        if (!File.Exists(projectPath))
            return;

        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        var fileEntries = new List<(string RelativePath, string FullPath)>();

        try
        {
            using var stream = File.OpenRead(projectPath);
            var doc = XDocument.Load(stream);

            var included = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var itemGroup in doc.Descendants().Where(e => e.Name.LocalName == "ItemGroup"))
            {
                foreach (var item in itemGroup.Elements())
                {
                    var localName = item.Name.LocalName;
                    if (localName != "Compile" && localName != "None" && localName != "Page" && localName != "AvaloniaResource")
                        continue;

                    var include = (string?)item.Attribute("Include");
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
                    if (!included.Add(fullPath) || !File.Exists(fullPath))
                        continue;

                    fileEntries.Add((normalizedInclude, fullPath));
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
                    fileEntries.Add((rel, Path.GetFullPath(f)));
                }
            }

            AddFileEntriesAsTree(projectNode, fileEntries);
        }
        catch
        {
            // Оставляем проект без дочерних файлов
        }
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

    private static void SortSolutionItemChildren(SolutionItem node, StringComparer comparer)
    {
        var list = node.Children;
        if (list.Count == 0)
            return;
        // Папки (FullPath == null) сначала по Title, затем файлы по Title
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

    private static string? ExtractPathFromSlnProjectLine(string line)
    {
        var parts = line.Split('"');
        if (parts.Length < 4)
            return null;
        return parts[3].Trim();
    }

    /// <summary>
    /// Ensures nested SolutionItem folders exist under <paramref name="root"/> based on a slnx Folder Name.
    /// Name often looks like "/src/" or "src" or "src/tests".
    /// </summary>
    private static SolutionItem EnsureSolutionFolders(SolutionItem root, string folderName)
    {
        var normalized = (folderName ?? "")
            .Replace('\\', '/')
            .Trim();

        // slnx tends to use "/src/" style. Keep it virtual: strip leading/trailing slashes.
        normalized = normalized.Trim('/');
        if (normalized.Length == 0)
            return root;

        var parts = normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var comparer = StringComparer.OrdinalIgnoreCase;
        var current = root;
        foreach (var part in parts)
        {
            var folder = current.Children.FirstOrDefault(c => c.FullPath is null && comparer.Equals(c.Title, part));
            if (folder is null)
            {
                folder = SolutionItem.CreateFolder(part);
                current.Children.Add(folder);
            }
            current = folder;
        }
        return current;
    }

    private static string? TryGetStringProperty(object obj, params string[] names)
    {
        var t = obj.GetType();
        foreach (var name in names)
        {
            var p = t.GetProperty(name);
            if (p is null)
                continue;
            if (p.PropertyType != typeof(string))
                continue;
            return (string?)p.GetValue(obj);
        }
        return null;
    }

    private static object? TryGetProperty(object obj, params string[] names)
    {
        var t = obj.GetType();
        foreach (var name in names)
        {
            var p = t.GetProperty(name);
            if (p is null)
                continue;
            return p.GetValue(obj);
        }
        return null;
    }
}
