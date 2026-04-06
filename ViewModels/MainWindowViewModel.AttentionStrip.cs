using System.Collections.ObjectModel;
using CascadeIDE.Features.UiChrome;

namespace CascadeIDE.ViewModels;

/// <summary>Единая полоса телеметрии (композитор сегментов build/tests/debug/git, ADR 0021).</summary>
public partial class MainWindowViewModel
{
    /// <summary>Упорядоченные сегменты для <see cref="Views.TelemetryStripView"/>; источник истины — <see cref="AttentionStripCompositor"/>.</summary>
    public ObservableCollection<AttentionStripSegment> AttentionStripSegments { get; } = new();

    private void RebuildAttentionStrip()
    {
        AttentionStripCompositor.Rebuild(
            AttentionStripSegments,
            TelemetryBuildText,
            TelemetryBuildCockpitShort,
            IsBuilding,
            TelemetryTestsText,
            TelemetryTestsCockpitShort,
            TelemetryDebugText,
            TelemetryDebugCockpitShort,
            Chrome.TelemetryGitText,
            Chrome.TelemetryGitCockpitShort);
    }
}
