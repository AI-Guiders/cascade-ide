using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public partial class OpenDocumentViewModel : ObservableObject
{
    public OpenDocumentViewModel(string filePath, string title, string content)
    {
        FilePath = filePath;
        Title = title;
        OriginalContent = content;
        _content = content;
    }

    public string FilePath { get; }
    public string Title { get; }
    public string OriginalContent { get; private set; }
    public string DisplayTitle => IsPinned ? $"[P] {Title}{(IsDirty ? "*" : "")}" : $"{Title}{(IsDirty ? "*" : "")}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isPinned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isDirty;

    [ObservableProperty]
    private int _groupIndex = 1;

    public void ReloadContent(string newContent)
    {
        OriginalContent = newContent ?? "";
        Content = OriginalContent;
        IsDirty = false;
    }
}
