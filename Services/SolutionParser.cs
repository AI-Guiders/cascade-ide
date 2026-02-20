using System.Xml.Linq;
using CascadeIDE.Models;

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

        if (string.Equals(ext, ".slnx", StringComparison.OrdinalIgnoreCase))
            return LoadSlnx(normalized, dir, name);

        if (string.Equals(ext, ".sln", StringComparison.OrdinalIgnoreCase))
            return LoadSln(normalized, dir, name);

        error = "Расширение не .slnx и не .sln.";
        return null;
    }

    private static SolutionItem LoadSlnx(string solutionPath, string baseDir, string solutionName)
    {
        var root = SolutionItem.CreateSolution(solutionName, solutionPath);
        using var stream = File.OpenRead(solutionPath);
        var doc = XDocument.Load(stream);

        foreach (var el in doc.Descendants().Where(e => e.Name.LocalName == "Project"))
        {
            var path = (string?)el.Attribute("Path");
            if (string.IsNullOrWhiteSpace(path))
                continue;

            var fullPath = Path.GetFullPath(Path.Combine(baseDir, path.Replace('/', Path.DirectorySeparatorChar)));
            var title = Path.GetFileName(path);
            var projectNode = SolutionItem.CreateProject(title, fullPath);
            AddProjectFileChildren(projectNode, fullPath);
            root.Children.Add(projectNode);
        }

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
}
