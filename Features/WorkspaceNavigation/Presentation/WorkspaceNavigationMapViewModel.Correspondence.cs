#nullable enable

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.WorkspaceNavigation.Presentation;

public sealed partial class WorkspaceNavigationMapViewModel
{
    public ObservableCollection<WorkspaceCorrespondenceDocItemVm> WorkspaceCorrespondenceDocItems { get; } = new();

    public ObservableCollection<WorkspaceReverseAnchorItemVm> WorkspaceReverseAnchorItems { get; } = new();

    [ObservableProperty]
    private string _workspaceReverseAnchorsStatus = "";

    [ObservableProperty]
    private string[] _workspaceCorrespondenceDocPaths = [];

    /// <summary>Канон: развернуть Mfd, страница CRS, refresh якоря (см. <c>show_correspondence_page</c>).</summary>
    [RelayCommand]
    private void ShowCorrespondencePage()
    {
        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.Correspondence);
        ScheduleWorkspaceNavigationMapRefresh();
    }

    [RelayCommand]
    private void OpenCorrespondenceDocument(object? parameter)
    {
        string? docPath;
        int? lineHint = null;

        if (parameter is WorkspaceReverseAnchorItemVm reverse)
        {
            docPath = reverse.DocPath;
            lineHint = reverse.DocLineHint;
        }
        else
        {
            docPath = parameter as string;
        }

        if (string.IsNullOrWhiteSpace(docPath))
            return;

        OpenRepoDocumentInMarkdownPreview(docPath, lineHint);
    }

    private void ApplyCorrespondenceDocAndReverseLists(
        string? workspaceRoot,
        string? navigationPath,
        string[] adrDocRepoPaths)
    {
        WorkspaceCorrespondenceDocPaths = adrDocRepoPaths;

        WorkspaceCorrespondenceDocItems.Clear();
        foreach (var p in adrDocRepoPaths)
        {
            WorkspaceCorrespondenceDocItems.Add(new WorkspaceCorrespondenceDocItemVm
            {
                DocPath = p,
                DisplayTitle = WorkspaceAdrMapResolver.GuessAdrPreviewTitle(p)
            });
        }

        WorkspaceReverseAnchorItems.Clear();
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(navigationPath))
        {
            WorkspaceReverseAnchorsStatus = adrDocRepoPaths.Length == 0
                ? ""
                : "Reverse anchors: укажите якорь файла в редакторе.";
            return;
        }

        var workspaceToml = RepositoryWorkspaceTomlLoader.TryLoad(workspaceRoot);
        var explicitAnchors = WorkspaceCorrespondenceCodeAnchorsLoader.LoadFromWorkspaceToml(workspaceToml, workspaceRoot);
        var reverse = DocReverseAnchorResolver.Resolve(workspaceRoot, navigationPath, adrDocRepoPaths, explicitAnchors);
        foreach (var m in reverse)
        {
            WorkspaceReverseAnchorItems.Add(new WorkspaceReverseAnchorItemVm
            {
                DocPath = m.DocPath,
                DisplayTitle = m.DocTitle,
                Excerpt = m.Excerpt,
                Provenance = m.Provenance,
                DocLineHint = m.DocLineHint
            });
        }

        WorkspaceReverseAnchorsStatus = reverse.Count == 0
            ? (adrDocRepoPaths.Length > 0
                ? "Reverse anchors: в связанных ADR явных ссылок на этот файл не найдено (bracket / scan)."
                : "")
            : $"Reverse anchors: {reverse.Count}";
    }

    private void OpenRepoDocumentInMarkdownPreview(string docPath, int? scrollToLine = null)
    {
        var wsRoot = _host.GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(wsRoot))
            return;

        if (!WorkspaceMarkdownPreviewOpener.TryOpenRepoDocument(
                wsRoot,
                docPath,
                (title, content, source) => _host.MarkdownPreviewTool.SetContent(title, content, source, scrollToLine),
                out _))
            return;

        _host.ApplyMfdRegionExpanded(true);
        _host.TryNavigateToMfdShellPage(MfdShellPage.MarkdownPreview);
    }
}
