using System.ComponentModel;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.Editor.Application;
using CascadeIDE.Features.Editor.Application.Presentation;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>
/// Wave 2: <see cref="EditorWorkspaceViewModel"/> + прокси на MWVM для существующих привязок и MCP.
/// </summary>
public partial class MainWindowViewModel
{
    /// <summary>Активный документ в редакторе (путь, текст, selection, HUD, брейкпоинты).</summary>
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

    public IReadOnlyList<int> BreakpointLinesInCurrentFile => Editor.BreakpointLinesInCurrentFile;

    public IReadOnlyList<int> AllBreakpointLinesInCurrentFile => Editor.AllBreakpointLinesInCurrentFile;

    public IReadOnlyList<int> GetAllBreakpointLinesForFile(string? filePath) =>
        Editor.GetAllBreakpointLinesForFile(filePath);

    public string? DebugPositionFile
    {
        get => Editor.DebugPositionFile;
        set => Editor.DebugPositionFile = value;
    }

    public int DebugPositionLine
    {
        get => Editor.DebugPositionLine;
        set => Editor.DebugPositionLine = value;
    }

    public int GetDebugCurrentLineForFile(string? filePath) => Editor.GetDebugCurrentLineForFile(filePath);

    public int DebugCurrentLineInCurrentFile => Editor.DebugCurrentLineInCurrentFile;

    public string? EditorHudBannerText
    {
        get => Editor.EditorHudBannerText;
        set => Editor.EditorHudBannerText = value;
    }

    public bool IsEditorHudBannerVisible => Editor.IsEditorHudBannerVisible;

    public bool IsEditorHudChromeVisible => IsEditorHudBannerVisible || ShowEditorHudControlFlowStrip;

    public IReadOnlyList<EditorTrailingInlayPart> GetEditorInlineHintsForFile(string filePath, string sourceText) =>
        Editor.GetEditorInlineHintsForFile(filePath, sourceText);

    public IReadOnlyList<EditorDebugHintStrip> GetEditorDebugHintsForFile(string filePath, string sourceText) =>
        Editor.GetEditorDebugHintsForFile(filePath, sourceText);

    internal void SetActiveEditorStabilizedHudHandler(Action<EditorInputDelta>? handler) =>
        Editor.SetActiveEditorStabilizedHudHandler(handler);

    internal void ClearActiveEditorStabilizedHudHandlerIfEquals(Action<EditorInputDelta>? handler) =>
        Editor.ClearActiveEditorStabilizedHudHandlerIfEquals(handler);

    internal bool TryPostEditorStabilizedInput(EditorInputDelta delta) =>
        Editor.TryPostEditorStabilizedInput(delta);

    internal void ShutdownEditorStabilizedInput() => Editor.ShutdownEditorStabilizedInput();

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
        else if (e.PropertyName is nameof(EditorWorkspaceViewModel.DebugPositionFile)
                 or nameof(EditorWorkspaceViewModel.DebugPositionLine))
        {
            OnPropertyChanged(nameof(DebugCurrentLineInCurrentFile));
        }
        else if (e.PropertyName == nameof(EditorWorkspaceViewModel.EditorHudBannerText))
        {
            OnPropertyChanged(nameof(IsEditorHudBannerVisible));
            OnPropertyChanged(nameof(IsEditorHudChromeVisible));
        }
    }

    /// <summary>Бывший <c>OnCurrentFilePathChanged</c> из DocumentsDock.</summary>
    internal void OnEditorCurrentFilePathChanged()
    {
        if (!TryPreserveControlFlowNavigateCaretOnFileChange())
            UpdateCodeNavigationMapCaretOffset(null);
        RefreshLocBadgeFromCurrentFile();
        Editor.RefreshEditorHudBanner();
        RefreshEditorHudControlFlowStrip();
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
        Editor.RefreshEditorHudBanner();
    }

    internal void OnWorkspaceDiagnosticsChangedForHud() => Editor.OnWorkspaceDiagnosticsChangedForHud();

    private void ScheduleEditorHudBannerRefresh() => Editor.ScheduleEditorHudBannerRefresh();

    internal void SetStabilizedEditorHudContext(EditorHudStabilizedContext? context) =>
        Editor.SetStabilizedEditorHudContext(context);

    private void RefreshEditorHudBanner() => Editor.RefreshEditorHudBanner();

    internal async Task<string> DebugLaunchInteractiveAsync() =>
        await Debug.DebugLaunchInteractiveAsync().ConfigureAwait(true);
}
