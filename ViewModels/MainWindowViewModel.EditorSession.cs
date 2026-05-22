using System.ComponentModel;
using CascadeIDE.Features.Editor;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Wave 2: <see cref="EditorWorkspaceViewModel"/> + прокси на MWVM для существующих привязок и MCP.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>Активный документ в редакторе (путь, текст, selection).</summary>
    public EditorWorkspaceViewModel Editor { get; private set; } = null!;

    public string? CurrentFilePath
    {
        get => Editor.CurrentFilePath;
        set => Editor.CurrentFilePath = value;
    }

    public bool IsLoadingCurrentFile
    {
        get => Editor.IsLoadingCurrentFile;
        set => Editor.IsLoadingCurrentFile = value;
    }

    public string EditorText
    {
        get => Editor.EditorText;
        set => Editor.EditorText = value;
    }

    public int? EditorSelectionStart
    {
        get => Editor.EditorSelectionStart;
        set => Editor.EditorSelectionStart = value;
    }

    public int? EditorSelectionLength
    {
        get => Editor.EditorSelectionLength;
        set => Editor.EditorSelectionLength = value;
    }

    public bool IsMarkdownFile => Editor.IsMarkdownFile;

    public bool IsMarkdownPreviewVisible => Editor.IsMarkdownPreviewVisible;

    private void OnEditorWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
            return;

        OnPropertyChanged(e.PropertyName);

        if (e.PropertyName == nameof(EditorWorkspaceViewModel.CurrentFilePath))
        {
            OnPropertyChanged(nameof(IsMarkdownFile));
            OnPropertyChanged(nameof(IsMarkdownPreviewVisible));
            OnPropertyChanged(nameof(BreakpointLinesInCurrentFile));
            OnPropertyChanged(nameof(AllBreakpointLinesInCurrentFile));
            OnPropertyChanged(nameof(DebugCurrentLineInCurrentFile));
        }
        else if (e.PropertyName == nameof(EditorWorkspaceViewModel.IsLoadingCurrentFile))
        {
            OnPropertyChanged(nameof(IsMarkdownPreviewVisible));
        }
    }

    /// <summary>Бывший <c>OnCurrentFilePathChanged</c> из DocumentsDock.</summary>
    internal void OnEditorCurrentFilePathChanged()
    {
        UpdateCodeNavigationMapCaretOffset(null);
        RefreshLocBadgeFromCurrentFile();
        RefreshEditorHudBanner();
        ScheduleWorkspaceNavigationMapRefresh();
    }

    internal void OnEditorIsLoadingCurrentFileChanged() =>
        OnPropertyChanged(nameof(IsMarkdownPreviewVisible));

    /// <summary>Бывший <c>OnEditorTextChanged</c> из DocumentsDock.</summary>
    internal void OnEditorTextChanged(string value)
    {
        Documents.ApplyEditorTextFromHost(value);
        OnPropertyChanged(nameof(EditorTextGroup2));
        OnPropertyChanged(nameof(EditorTextGroup3));
        RefreshEditorHudBanner();
    }
}
