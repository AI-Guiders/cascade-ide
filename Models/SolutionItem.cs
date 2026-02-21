using System.Collections.ObjectModel;

namespace CascadeIDE.Models;

public sealed class SolutionItem
{
    public string Title { get; }
    public string? FullPath { get; }
    public bool IsFolder => Children.Count > 0 && FullPath is null;
    public ObservableCollection<SolutionItem> Children { get; } = [];

    /// <summary>Ключ иконки для UI: solution, project, folder, file, file_cs, file_json, file_md, file_xml, file_txt и т.д.</summary>
    public string IconKey => GetIconKey();

    private string GetIconKey()
    {
        if (FullPath is null)
            return Children.Count > 0 ? "folder" : "file";
        var p = FullPath.AsSpan();
        if (p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            return "solution";
        if (p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return "project";
        var ext = Path.GetExtension(FullPath);
        if (string.IsNullOrEmpty(ext) || ext.Length <= 1) return "file";
        return "file_" + ext[1..].ToLowerInvariant();
    }

    private SolutionItem(string title, string? fullPath)
    {
        Title = title;
        FullPath = fullPath;
    }

    public static SolutionItem CreateSolution(string title, string slnPath)
        => new(title, slnPath);

    public static SolutionItem CreateProject(string title, string projectPath)
        => new(title, projectPath);

    public static SolutionItem CreateFile(string title, string filePath)
        => new(title, filePath);

    public static SolutionItem CreateFolder(string title)
        => new(title, null);
}
