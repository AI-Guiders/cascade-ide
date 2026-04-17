using System.Collections.ObjectModel;
using CascadeIDE.Cockpit.Channels.WorkspaceHealth;
using CascadeIDE.Cockpit.Composition.WorkspaceHealth;

namespace CascadeIDE.ViewModels;

/// <summary>Связка с Workspace Health.</summary>
public partial class MainWindowViewModel
{
    /// <summary>Упорядоченные сегменты для <see cref="Views.WorkspaceHealthStripView"/> (поверхность); строит <see cref="IWorkspaceHealthSurfaceCompositor"/> из снимка канала (ADR 0036 п.1→п.3).</summary>
    public ObservableCollection<WorkspaceHealthSegment> WorkspaceHealthSegments { get; } = new();

    private void RebuildWorkspaceHealth()
    {
        var snapshot = _workspaceHealth.Build(WorkspaceHealthChannelContext.Default);
        _workspaceHealthSurfaceCompositor.Compose(
            WorkspaceHealthSegments,
            snapshot,
            new WorkspaceHealthSurfaceDecision(Enabled: true));
        OnPropertyChanged(nameof(WorkspaceHealthMountPayload));
        OnPropertyChanged(nameof(PfdWorkspaceHealthMountContext));
        OnPropertyChanged(nameof(MfdWorkspaceHealthMountContext));
    }
}
