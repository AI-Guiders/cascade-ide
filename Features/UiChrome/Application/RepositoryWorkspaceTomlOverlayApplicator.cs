#nullable enable

using CascadeIDE.Cockpit.Composition.HostSurface;
using CascadeIDE.Features.UiChrome;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;

namespace CascadeIDE.Features.UiChrome.Application;

/// <summary>
/// Накладывает <c>.cascade/workspace.toml</c> репозитория на runtime-метрики UI (слой бандла UiModes + repo, ADR 0021 §2.1).
/// </summary>
public static class RepositoryWorkspaceTomlOverlayApplicator
{
    public static void Apply(RepositoryWorkspaceToml? bundleLayer, string? solutionDirectory)
    {
        var repo = RepositoryWorkspaceTomlLoader.TryLoad(solutionDirectory);
        var merged = RepositoryWorkspaceTomlMerger.Merge(bundleLayer, repo);
        UiWorkspaceLayoutRuntimeMetrics.ApplyWorkspaceToml(merged);
        AttentionZonePanelRuntime.ApplyWorkspaceToml(merged);
        MarkdownPreviewPlacementRuntime.ApplyWorkspaceToml(merged);
        LocLimitsRuntime.ApplyWorkspaceToml(merged);
        InstrumentPlacementRuntime.ApplyWorkspaceInstrumentRouting(merged?.Routing?.Instruments);
    }
}
