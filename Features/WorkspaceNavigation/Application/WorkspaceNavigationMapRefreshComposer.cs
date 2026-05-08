#nullable enable
using System.Text.Json;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Models;
using CascadeIDE.Services.Navigation;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Разбор JSON refresh карты намерений и сборка сцены/related-строк (до маршала на UI).
/// </summary>
public static class WorkspaceNavigationMapRefreshComposer
{
    public sealed record Dependencies(
        CodeNavigationMapCompositor MapCompositor,
        TraceFlowChannelCoordinator TraceFlowCoordinator,
        ITraceFlowCdsRouter TraceFlowCdsRouter,
        ITraceFlowSurfaceCompositor TraceFlowSurfaceCompositor);

    public sealed record TraceSignals(int ImpactedTestsBadge, string? LastTestSummary);

    public sealed record DryResult(
        string Status,
        string AnchorLabel,
        CodeNavigationMapGraphSceneVm? Scene,
        double GraphHeight,
        int AccentCount,
        IReadOnlyList<WorkspaceNavigationMapOrchestrator.RelatedRow> ListRows);

    /// <summary>Разбирает <paramref name="json"/> и собирает сцену или related-список; при сбое — статус ошибки без исключений наружу.</summary>
    public static DryResult Compose(
        Dependencies deps,
        string json,
        bool useSubgraphMode,
        bool wantList,
        string? currentPath,
        string? solutionPath,
        string normalizedLevel,
        double graphWidth,
        double graphHeight,
        CodeNavigationMapDetailLevel mapDetailLevel,
        TraceSignals trace,
        Func<CockpitSurfaceState> cockpitSurfaceSnapshot)
    {
        var rows = new List<WorkspaceNavigationMapOrchestrator.RelatedRow>();
        var status = string.Empty;
        var anchorLabel = "—";
        CodeNavigationMapGraphSceneVm? scene = null;
        var graphPreferredHeight = CodeNavigationMapCompositor.DefaultHeightFile;
        var accentCount = 0;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out _))
            {
                status = WorkspaceNavigationMapOrchestrator.ResolveErrorStatus(root, currentPath);
            }
            else if (useSubgraphMode && CodeNavigationMapSubgraphJson.TryParse(json, out var subgraph, out _))
            {
                var viewport = new SkiaInstrumentViewport(graphWidth, graphHeight);
                var composed = deps.MapCompositor.Compose(
                    new CodeNavigationMapCompositionIntent(subgraph!, normalizedLevel, mapDetailLevel),
                    viewport);
                if (normalizedLevel == CodeNavigationMapLevelKind.ControlFlow)
                {
                    var channelPayload = deps.TraceFlowCoordinator.Build(new TraceFlowChannelContext(
                        subgraph!,
                        trace.ImpactedTestsBadge,
                        trace.LastTestSummary ?? ""));
                    var cds = cockpitSurfaceSnapshot();
                    var cdsDecision = deps.TraceFlowCdsRouter.Route(new TraceFlowCdsRouteInput(cds, normalizedLevel));
                    scene = deps.TraceFlowSurfaceCompositor.Compose(composed.Scene, channelPayload, cdsDecision);
                }
                else
                {
                    scene = composed.Scene;
                }

                graphPreferredHeight = composed.PreferredHeight;
                var satCount = Math.Max(0, scene!.Nodes.Count - 1);
                accentCount = satCount;
                anchorLabel = WorkspaceNavigationMapOrchestrator.ResolveAnchorLabelFromSubgraph(subgraph!);

                if (wantList)
                {
                    rows.AddRange(WorkspaceNavigationMapOrchestrator.BuildRowsFromSubgraph(subgraph!, solutionPath));
                    accentCount = Math.Max(accentCount, rows.Count);
                    status = WorkspaceNavigationMapOrchestrator.ResolveEmptyStatus(rows, status, wantList: true);
                }
            }
            else if (wantList)
            {
                anchorLabel = WorkspaceNavigationMapOrchestrator.ResolveAnchorLabelFromRelatedRoot(root);
                rows.AddRange(WorkspaceNavigationMapOrchestrator.BuildRowsFromRelatedRoot(root));
                accentCount = rows.Count;
                status = WorkspaceNavigationMapOrchestrator.ResolveEmptyStatus(rows, status, wantList: true);
            }

            return new DryResult(status, anchorLabel, scene, graphPreferredHeight, accentCount, rows);
        }
        catch
        {
            return new DryResult(
                "Не удалось разобрать ответ навигации.",
                "—",
                null,
                CodeNavigationMapCompositor.DefaultHeightFile,
                0,
                []);
        }
    }
}
