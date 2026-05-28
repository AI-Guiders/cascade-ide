#nullable enable

using Avalonia.Controls;
using AvaloniaEdit;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Features.WorkspaceNavigation.Presentation;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.ViewModels;

/// <summary>Мост к <see cref="WorkspaceNavigationMapViewModel"/> (ADR 0039): host + lifecycle, без прокси-привязок.</summary>
public partial class MainWindowViewModel
{
    public WorkspaceNavigationMapViewModel NavigationMap { get; private set; } = null!;

    private void InitializeWorkspaceNavigationMap() =>
        NavigationMap = new WorkspaceNavigationMapViewModel(this);

    internal void UpdateCodeNavigationMapCaretOffset(int? offset) =>
        NavigationMap.UpdateCodeNavigationMapCaretOffset(offset);

    internal void NotifyCodeNavigationMapGraphViewportWidthChanged(double width) =>
        NavigationMap.NotifyCodeNavigationMapGraphViewportWidthChanged(width);

    internal void ScheduleWorkspaceNavigationMapRefresh() =>
        NavigationMap.ScheduleWorkspaceNavigationMapRefresh();

    internal bool TryPreserveControlFlowNavigateCaretOnFileChange() =>
        NavigationMap.TryPreserveControlFlowNavigateCaretOnFileChange();

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
