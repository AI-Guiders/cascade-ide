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
            new AttentionStripInputSnapshot(
                Build: new AttentionStripSegmentInput(
                    TelemetryBuildText,
                    TelemetryBuildCockpitShort,
                    IsBuilding),
                Tests: new AttentionStripSegmentInput(TelemetryTestsText, TelemetryTestsCockpitShort),
                Debug: new AttentionStripSegmentInput(TelemetryDebugText, TelemetryDebugCockpitShort),
                Git: new AttentionStripSegmentInput(Chrome.TelemetryGitText, Chrome.TelemetryGitCockpitShort)));
    }
}
