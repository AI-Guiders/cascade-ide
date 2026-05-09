using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using CascadeIDE.Models;
using Microsoft.VisualStudio.SolutionPersistence.Model;
using Microsoft.VisualStudio.SolutionPersistence.Serializer;

namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>
/// Загрузка .sln / .slnx / .slnf, либо одного .csproj/.fsproj (без solution-файла), и построение дерева <see cref="SolutionItem"/>.
/// Файлы внутри .csproj — <see cref="ProjectFileTreeBuilder"/>.
/// </summary>
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
            normalized = CanonicalFilePath.Normalize(solutionPath);
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

        if (string.Equals(ext, ".csproj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ext, ".fsproj", StringComparison.OrdinalIgnoreCase))
            return LoadStandaloneProject(normalized, dir, name, out error);

        error = "Расширение не поддерживается: ожидается .sln, .slnx, .slnf или .csproj/.fsproj.";
        return null;
    }

    /// <summary>Один файл проекта без .sln: корень обозревателя — виртуальное «решение» с одним проектом и деревом файлов.</summary>
    private static SolutionItem? LoadStandaloneProject(
        string projectPath,
        string projectParentDir,
        string projectNameWithoutExt,
        out string? error)
    {
        error = null;
        try
        {
            var root = SolutionItem.CreateSolution(projectNameWithoutExt, projectPath);
            var projectNode = SolutionItem.CreateProject(Path.GetFileName(projectPath), projectPath);
            ProjectFileTreeBuilder.AddProjectFileChildren(projectNode, projectPath, projectParentDir);
            root.Children.Add(projectNode);
            ProjectFileTreeBuilder.SortSolutionItemChildren(root, StringComparer.OrdinalIgnoreCase);
            return root;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
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
            var folderFactory = new SolutionFolderNodeFactory(root);
            foreach (var project in model.SolutionProjects)
            {
                var projectPath = project.FilePath;
                if (string.IsNullOrWhiteSpace(projectPath))
                    continue;

                var fullPath = CanonicalFilePath.Normalize(Path.Combine(baseDir, projectPath.Replace('/', Path.DirectorySeparatorChar)));
                var title = Path.GetFileName(projectPath);
                var projectNode = SolutionItem.CreateProject(title, fullPath);
                ProjectFileTreeBuilder.AddProjectFileChildren(projectNode, fullPath, baseDir);

                SolutionItem parent = root;
                try
                {
                    var parentObj =
                        TryGetProperty(project, "Parent") ??
                        TryGetProperty(project, "ParentFolder") ??
                        TryGetProperty(project, "SolutionFolder");

                    if (parentObj is not null)
                        parent = folderFactory.GetOrCreate(parentObj);
                }
                catch
                {
                    parent = root;
                }

                parent.Children.Add(projectNode);
            }

            ProjectFileTreeBuilder.SortSolutionItemChildren(root, StringComparer.OrdinalIgnoreCase);
            return root;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>Кэш папок решения по объектам API SolutionPersistence (сравнение по ссылке).</summary>
    private sealed class SolutionFolderNodeFactory
    {
        private readonly SolutionItem _root;
        private readonly Dictionary<object, SolutionItem> _cache = new(ReferenceEqualityComparer.Instance);

        public SolutionFolderNodeFactory(SolutionItem root) => _root = root;

        public SolutionItem GetOrCreate(object folderObj)
        {
            if (_cache.TryGetValue(folderObj, out var existing))
                return existing;

            var folderName = TryGetStringProperty(folderObj, "Name", "FolderName", "Path") ?? "Folder";
            var folderNode = EnsureSolutionFolders(_root, folderName);
            _cache[folderObj] = folderNode;
            return folderNode;
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

                var fullPath = CanonicalFilePath.Normalize(Path.Combine(baseDir, path.Replace('/', Path.DirectorySeparatorChar)));
                var title = Path.GetFileName(path);
                var projectNode = SolutionItem.CreateProject(title, fullPath);
                ProjectFileTreeBuilder.AddProjectFileChildren(projectNode, fullPath, baseDir);
                parentFolder.Children.Add(projectNode);
            }
        }

        VisitContainer(solutionEl, root);
        ProjectFileTreeBuilder.SortSolutionItemChildren(root, StringComparer.OrdinalIgnoreCase);

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

            var fullPath = CanonicalFilePath.Normalize(Path.Combine(baseDir, path.Replace('\\', Path.DirectorySeparatorChar)));
            var title = Path.GetFileName(path);
            var projectNode = SolutionItem.CreateProject(title, fullPath);
            ProjectFileTreeBuilder.AddProjectFileChildren(projectNode, fullPath, baseDir);
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
            if (!TryGetSlnfBaseSolutionAndAllowedProjects(doc, baseDir, out var baseSolutionPath, out var allowedProjects, out var parseError))
            {
                error = parseError;
                return null;
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
            ProjectFileTreeBuilder.SortSolutionItemChildren(baseRoot, StringComparer.OrdinalIgnoreCase);
            return baseRoot;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>Читает <c>solution.path</c> и набор путей проектов из .slnf (относительно каталога базового .sln).</summary>
    private static bool TryGetSlnfBaseSolutionAndAllowedProjects(
        JsonDocument doc,
        string slnfBaseDir,
        [NotNullWhen(true)] out string? baseSolutionPath,
        [NotNullWhen(true)] out HashSet<string>? allowedProjects,
        [NotNullWhen(false)] out string? error)
    {
        error = null;
        baseSolutionPath = null;
        allowedProjects = null;
        if (!doc.RootElement.TryGetProperty("solution", out var solutionEl))
        {
            error = "Некорректный .slnf: нет поля solution.";
            return false;
        }

        string baseSolutionRel = "";
        if (solutionEl.TryGetProperty("path", out var pathEl) && pathEl.ValueKind == JsonValueKind.String)
            baseSolutionRel = pathEl.GetString() ?? "";

        if (string.IsNullOrWhiteSpace(baseSolutionRel))
        {
            error = "Некорректный .slnf: solution.path пуст.";
            return false;
        }

        var resolvedBase = CanonicalFilePath.Normalize(Path.Combine(slnfBaseDir, baseSolutionRel.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(resolvedBase))
        {
            error = "Базовый solution не найден: " + resolvedBase;
            return false;
        }

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (solutionEl.TryGetProperty("projects", out var projectsEl) && projectsEl.ValueKind == JsonValueKind.Array)
        {
            var baseSlnDir = Path.GetDirectoryName(resolvedBase) ?? slnfBaseDir;
            foreach (var p in projectsEl.EnumerateArray())
            {
                if (p.ValueKind != JsonValueKind.String)
                    continue;
                var rel = (p.GetString() ?? "").Trim();
                if (rel.Length == 0)
                    continue;
                var full = CanonicalFilePath.Normalize(Path.Combine(baseSlnDir, rel.Replace('/', Path.DirectorySeparatorChar)));
                allowed.Add(full);
            }
        }

        baseSolutionPath = resolvedBase;
        allowedProjects = allowed;
        return true;
    }

    private static bool PruneTreeToAllowedProjects(SolutionItem node, HashSet<string> allowedProjects)
    {
        // Returns whether this subtree contains any allowed project.
        if (node.FullPath is { } projPath && projPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var fp = projPath;
            if (CanonicalFilePath.TryNormalize(fp, out var norm))
                fp = norm;
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
