using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CascadeIDE.Models;

public sealed class SolutionItem : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isVisible = true;

    public string Title { get; }
    public string? FullPath { get; }
    public bool IsFolder => Children.Count > 0 && FullPath is null;
    public ObservableCollection<SolutionItem> Children { get; } = [];

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
                return;
            _isVisible = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Ключ иконки для UI: solution, project, folder, file, file_cs, file_json, file_md, file_xml, file_txt и т.д.</summary>
    public string IconKey => GetIconKey();

    public event PropertyChangedEventHandler? PropertyChanged;

    private string GetIconKey()
    {
        if (FullPath is null)
            return Children.Count > 0 ? "folder" : "file";
        if (Directory.Exists(FullPath))
            return "folder";
        var p = FullPath.AsSpan();
        if (p.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            return "solution";
        if (p.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            return "file_csproj";
        if (p.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
            return "file_fsproj";
        if (p.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase))
            return "file_vbproj";
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

    /// <summary>Корень обозревателя при открытии каталога как workspace (без .sln). <see cref="IconKey"/> — папка.</summary>
    public static SolutionItem CreateFolderWorkspaceRoot(string title, string folderPath)
        => new(title, folderPath);

    public static SolutionItem CreateProject(string title, string projectPath)
        => new(title, projectPath);

    public static SolutionItem CreateFile(string title, string filePath)
        => new(title, filePath);

    public static SolutionItem CreateFolder(string title)
        => new(title, null);

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
