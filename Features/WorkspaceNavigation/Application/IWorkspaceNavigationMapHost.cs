#nullable enable

using AvaloniaEdit;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.Editor;
using CascadeIDE.Features.Markdown;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Контекст shell для <see cref="Presentation.WorkspaceNavigationMapViewModel"/> (PFD navigation map).</summary>
public interface IWorkspaceNavigationMapHost
{
    MainWindowViewModel Shell { get; }

    SolutionWorkspaceViewModel Workspace { get; }

    DocumentsWorkspaceViewModel Documents { get; }

    EditorWorkspaceViewModel Editor { get; }

    MarkdownPreviewToolViewModel MarkdownPreviewTool { get; }

    CascadeIdeSettings Settings { get; }

    Services.IIdeMcpActions IdeMcp { get; }

    string? GetWorkspacePath();

    string? CurrentFilePath { get; }

    string EditorText { get; }

    int? EditorSelectionStart { get; }

    int ImpactedTestsBadge { get; }

    string? LastTestSummary { get; }

    Func<string, IReadOnlyList<string>, Task<string?>>? RequestPickFeatureDocAsync { get; }

    void ApplyMfdRegionExpanded(bool expanded);

    void TryNavigateToMfdShellPage(MfdShellPage page);

    void SaveSettingsIfChanged();

    void ScheduleEditorHudBannerRefresh();

    IEnumerable<TextEditor> EnumerateEditorsForPath(string? currentPath);

    void RevealEditorRange(string? path, int startLine, int endLine, int? column);
}
