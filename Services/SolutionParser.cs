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
            root.Children.Add(SolutionItem.CreateProject(title, fullPath));
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
            root.Children.Add(SolutionItem.CreateProject(title, fullPath));
        }

        return root;
    }

    private static string? ExtractPathFromSlnProjectLine(string line)
    {
        var parts = line.Split('"');
        if (parts.Length < 4)
            return null;
        return parts[3].Trim();
    }
}
