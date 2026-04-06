using System.Collections.ObjectModel;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    /// <summary>Упорядоченные сегменты для <see cref="Views.TelemetryStripView"/>; источник истины — <see cref="WorkspaceTelemetryCompositor"/>.</summary>
    public ObservableCollection<WorkspaceTelemetrySegment> WorkspaceTelemetrySegments { get; } = new();

    private void RebuildWorkspaceTelemetry()
    {
        WorkspaceTelemetryCompositor.Rebuild(WorkspaceTelemetrySegments, _workspaceTelemetry.GetSnapshot());
    }
}
