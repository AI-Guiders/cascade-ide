using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public partial class OpenDocumentViewModel : ObservableObject
{
    public const string SharedWithAgentSuffix = " · shared";

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

    public string DisplayTitle
    {
        get
        {
            var core = IsPinned ? $"[P] {Title}" : Title;
            if (IsDirty)
                core += "*";
            if (IsSharedWithAgent)
                core += SharedWithAgentSuffix;
            return core;
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private string _content;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isPinned;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isDirty;

    /// <summary>Co-presence with agent open buffer (shared-LATEST latch → glass tab chrome).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    private bool _isSharedWithAgent;

    [ObservableProperty]
    private int _groupIndex = 1;

    public void ReloadContent(string newContent)
    {
        OriginalContent = newContent ?? "";
        Content = OriginalContent;
        IsDirty = false;
    }
}
