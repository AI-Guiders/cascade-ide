using System.Collections.ObjectModel;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Упорядоченные сегменты для <see cref="Views.WorkspaceHealthStripView"/>; источник истины — <see cref="WorkspaceHealthCompositor"/>.</summary>
    public ObservableCollection<WorkspaceHealthSegment> WorkspaceHealthSegments { get; } = new();

    private void RebuildWorkspaceHealth()
    {
        WorkspaceHealthCompositor.Rebuild(WorkspaceHealthSegments, _workspaceHealth.GetSnapshot());
    }
}
