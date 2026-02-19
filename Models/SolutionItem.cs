using System.Collections.ObjectModel;

namespace CascadeIDE.Models;

public sealed class SolutionItem
{
    public string Title { get; }
    public string? FullPath { get; }
    public bool IsFolder => Children.Count > 0 && FullPath is null;
    public ObservableCollection<SolutionItem> Children { get; } = [];

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
