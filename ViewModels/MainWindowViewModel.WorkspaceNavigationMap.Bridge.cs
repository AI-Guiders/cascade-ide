#nullable enable

using Avalonia.Controls;
using AvaloniaEdit;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Features.WorkspaceNavigation.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CascadeIDE.ViewModels;

/// <summary>Мост к <see cref="WorkspaceNavigationMapViewModel"/> (ADR 0039).</summary>
public partial class MainWindowViewModel
{
    public WorkspaceNavigationMapViewModel NavigationMap { get; private set; } = null!;

    private void InitializeWorkspaceNavigationMap()
    {
        NavigationMap = new WorkspaceNavigationMapViewModel(this);
        NavigationMap.PropertyChanged += OnNavigationMapPropertyChanged;
    }

    private void OnNavigationMapPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName))
            return;

        OnPropertyChanged(e.PropertyName);
    }

    internal void UpdateCodeNavigationMapCaretOffset(int? offset) =>
        NavigationMap.UpdateCodeNavigationMapCaretOffset(offset);

    internal void NotifyCodeNavigationMapGraphViewportWidthChanged(double width) =>
        NavigationMap.NotifyCodeNavigationMapGraphViewportWidthChanged(width);

    internal void ScheduleWorkspaceNavigationMapRefresh() =>
        NavigationMap.ScheduleWorkspaceNavigationMapRefresh();

    internal bool TryPreserveControlFlowNavigateCaretOnFileChange() =>
        NavigationMap.TryPreserveControlFlowNavigateCaretOnFileChange();

    public ObservableCollection<WorkspaceNavigationMapItemVm> WorkspaceNavigationMapItems =>
        NavigationMap.WorkspaceNavigationMapItems;

    public string[] CodeNavigationMapPresentationOptions => NavigationMap.CodeNavigationMapPresentationOptions;

    public string[] CodeNavigationMapLevelOptions => NavigationMap.CodeNavigationMapLevelOptions;

    public string[] CodeNavigationMapControlFlowMainAxisOptions =>
        NavigationMap.CodeNavigationMapControlFlowMainAxisOptions;

    public string? WorkspaceNavigationMapCfAnchorFullPath
    {
        get => NavigationMap.WorkspaceNavigationMapCfAnchorFullPath;
        set => NavigationMap.WorkspaceNavigationMapCfAnchorFullPath = value;
    }

    public CodeNavigationMapGraphSceneVm? CodeNavigationMapGraphScene
    {
        get => NavigationMap.CodeNavigationMapGraphScene;
        set => NavigationMap.CodeNavigationMapGraphScene = value;
    }

    public double CodeNavigationMapGraphHeight
    {
        get => NavigationMap.CodeNavigationMapGraphHeight;
        set => NavigationMap.CodeNavigationMapGraphHeight = value;
    }

    public double CodeNavigationMapGraphWidth
    {
        get => NavigationMap.CodeNavigationMapGraphWidth;
        set => NavigationMap.CodeNavigationMapGraphWidth = value;
    }

    public string CodeNavigationMapPresentation
    {
        get => NavigationMap.CodeNavigationMapPresentation;
        set => NavigationMap.CodeNavigationMapPresentation = value;
    }

    public string CodeNavigationMapLevel
    {
        get => NavigationMap.CodeNavigationMapLevel;
        set => NavigationMap.CodeNavigationMapLevel = value;
    }

    public string CodeNavigationMapControlFlowMainAxis
    {
        get => NavigationMap.CodeNavigationMapControlFlowMainAxis;
        set => NavigationMap.CodeNavigationMapControlFlowMainAxis = value;
    }

    public string WorkspaceNavigationMapStatus
    {
        get => NavigationMap.WorkspaceNavigationMapStatus;
        set => NavigationMap.WorkspaceNavigationMapStatus = value;
    }

    public string WorkspaceNavigationMapAnchorLabel
    {
        get => NavigationMap.WorkspaceNavigationMapAnchorLabel;
        set => NavigationMap.WorkspaceNavigationMapAnchorLabel = value;
    }

    public int WorkspaceNavigationMapRelatedCount
    {
        get => NavigationMap.WorkspaceNavigationMapRelatedCount;
        set => NavigationMap.WorkspaceNavigationMapRelatedCount = value;
    }

    public string WorkspaceNavigationMapHciOrientationLine
    {
        get => NavigationMap.WorkspaceNavigationMapHciOrientationLine;
        set => NavigationMap.WorkspaceNavigationMapHciOrientationLine = value;
    }

    public string WorkspaceAdrCorrespondenceLine
    {
        get => NavigationMap.WorkspaceAdrCorrespondenceLine;
        set => NavigationMap.WorkspaceAdrCorrespondenceLine = value;
    }

    public string? WorkspaceAdrCorrespondenceFirstDocPath
    {
        get => NavigationMap.WorkspaceAdrCorrespondenceFirstDocPath;
        set => NavigationMap.WorkspaceAdrCorrespondenceFirstDocPath = value;
    }

    public string WorkspaceFeatureLine
    {
        get => NavigationMap.WorkspaceFeatureLine;
        set => NavigationMap.WorkspaceFeatureLine = value;
    }

    public string WorkspaceDocsCoverageLine
    {
        get => NavigationMap.WorkspaceDocsCoverageLine;
        set => NavigationMap.WorkspaceDocsCoverageLine = value;
    }

    public string[] WorkspaceFeatureDocPaths
    {
        get => NavigationMap.WorkspaceFeatureDocPaths;
        set => NavigationMap.WorkspaceFeatureDocPaths = value;
    }

    public bool ShowCodeNavigationMapList => NavigationMap.ShowCodeNavigationMapList;

    public bool ShowCodeNavigationMapListOnPfd => NavigationMap.ShowCodeNavigationMapListOnPfd;

    public bool ShowCodeNavigationMapGraph => NavigationMap.ShowCodeNavigationMapGraph;

    public Avalonia.Controls.GridLength CodeNavigationMapListAreaRowHeight =>
        NavigationMap.CodeNavigationMapListAreaRowHeight;

    public bool ShowCodeNavigationMapGraphClickHint => NavigationMap.ShowCodeNavigationMapGraphClickHint;

    public string WorkspaceNavigationMapRelatedBadge => NavigationMap.WorkspaceNavigationMapRelatedBadge;

    public bool WorkspaceNavigationMapHasRelated => NavigationMap.WorkspaceNavigationMapHasRelated;

    public string CodeNavigationMapSettingsSummaryLine => NavigationMap.CodeNavigationMapSettingsSummaryLine;

    public bool IsControlFlowEditorVirtualSpacingActiveForFile(string? filePath) =>
        NavigationMap.IsControlFlowEditorVirtualSpacingActiveForFile(filePath);

    public IReadOnlyList<ControlFlowLineVisual>? GetControlFlowGutterLineVisualsForFile(string? filePath) =>
        NavigationMap.GetControlFlowGutterLineVisualsForFile(filePath);

    public void CycleCodeNavigationMapPresentation() => NavigationMap.CycleCodeNavigationMapPresentation();

    public void CycleCodeNavigationMapLevel() => NavigationMap.CycleCodeNavigationMapLevel();

    public void SetCodeNavigationMapLevel(string level) => NavigationMap.SetCodeNavigationMapLevel(level);

    public void CycleCodeNavigationMapRelatedGraphLayout() =>
        NavigationMap.CycleCodeNavigationMapRelatedGraphLayout();

    public void CycleCodeNavigationMapDetailLevel() => NavigationMap.CycleCodeNavigationMapDetailLevel();

    public IRelayCommand OpenWorkspaceNavigationRelatedCommand => NavigationMap.OpenWorkspaceNavigationRelatedCommand;

    public IRelayCommand OpenWorkspaceAdrCorrespondenceCommand => NavigationMap.OpenWorkspaceAdrCorrespondenceCommand;

    public IAsyncRelayCommand OpenWorkspaceFeatureDocsCommand => NavigationMap.OpenWorkspaceFeatureDocsCommand;

    public IAsyncRelayCommand OpenDocsTemplateCommand => NavigationMap.OpenDocsTemplateCommand;

    MainWindowViewModel IWorkspaceNavigationMapHost.Shell => this;

    SolutionWorkspaceViewModel IWorkspaceNavigationMapHost.Workspace => Workspace;

    DocumentsWorkspaceViewModel IWorkspaceNavigationMapHost.Documents => Documents;

    EditorWorkspaceViewModel IWorkspaceNavigationMapHost.Editor => Editor;

    MarkdownPreviewToolViewModel IWorkspaceNavigationMapHost.MarkdownPreviewTool => MarkdownPreviewTool;

    CascadeIdeSettings IWorkspaceNavigationMapHost.Settings => _settings;

    IIdeMcpActions IWorkspaceNavigationMapHost.IdeMcp => IdeMcp;

    HybridIndexOrchestrator IWorkspaceNavigationMapHost.HybridIndex => _hybridIndex;

    bool IWorkspaceNavigationMapHost.IsPfdRegionExpanded => Shell.IsPfdRegionExpanded;

    string? IWorkspaceNavigationMapHost.GetWorkspacePath() => GetWorkspacePath();

    string? IWorkspaceNavigationMapHost.CurrentFilePath => CurrentFilePath;

    string IWorkspaceNavigationMapHost.EditorText => EditorText;

    int? IWorkspaceNavigationMapHost.EditorSelectionStart => EditorSelectionStart;

    int IWorkspaceNavigationMapHost.ImpactedTestsBadge => ImpactedTestsBadge;

    string? IWorkspaceNavigationMapHost.LastTestSummary => LastTestSummary;

    Func<string, IReadOnlyList<string>, Task<string?>>? IWorkspaceNavigationMapHost.RequestPickFeatureDocAsync =>
        RequestPickFeatureDocAsync;

    void IWorkspaceNavigationMapHost.ApplyMfdRegionExpanded(bool expanded) => ApplyMfdRegionExpanded(expanded);

    void IWorkspaceNavigationMapHost.TryNavigateToMfdShellPage(MfdShellPage page) => TryNavigateToMfdShellPage(page);

    void IWorkspaceNavigationMapHost.SaveSettingsIfChanged() => SaveSettingsIfChanged();

    void IWorkspaceNavigationMapHost.ScheduleEditorHudBannerRefresh() => ScheduleEditorHudBannerRefresh();

    IEnumerable<TextEditor> IWorkspaceNavigationMapHost.EnumerateEditorsForPath(string? currentPath) =>
        NavigationMap.EnumerateEditorsForPath(currentPath);

    void IWorkspaceNavigationMapHost.RevealEditorRange(string? path, int startLine, int endLine, int? column) =>
        _revealEditorRangeAction?.Invoke(path, startLine, endLine, column);
}
