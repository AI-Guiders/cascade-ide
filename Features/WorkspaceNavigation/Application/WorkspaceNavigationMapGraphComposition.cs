#nullable enable
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Фабрика композиции graph-backed карты для feature-среза (MWVM не держит детали pipeline).</summary>
public static class WorkspaceNavigationMapGraphComposition
{
    public static CodeNavigationMapCompositor CreateDefaultCompositor() => new();

    public static WorkspaceNavigationMapRefreshComposer.Dependencies CreateRefreshDependencies(
        TraceFlowChannelCoordinator traceFlowCoordinator,
        ITraceFlowCdsRouter traceFlowCdsRouter,
        ITraceFlowSurfaceCompositor traceFlowSurfaceCompositor) =>
        new(
            CreateDefaultCompositor(),
            traceFlowCoordinator,
            traceFlowCdsRouter,
            traceFlowSurfaceCompositor);
}
