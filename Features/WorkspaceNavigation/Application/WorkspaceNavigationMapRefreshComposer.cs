#nullable enable
using System.Text.Json;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.Contracts;
using CascadeIDE.Features.Documents;
using CascadeIDE.Models;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Разбор JSON refresh карты намерений и сборка сцены/related-строк (до маршала на UI).
/// </summary>
[ComputingUnit]
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
        IReadOnlyList<WorkspaceNavigationMapOrchestrator.RelatedRow> ListRows,
        string? CfAnchorFullPath = null);

    /// <summary>Разбирает <paramref name="json"/> и собирает сцену или related-список; при сбое — статус ошибки без исключений наружу.</summary>
    /// <param name="cockpitSurfaceCapturedOnUi">Снимок CDS с UI-потока; обязателен, если ветка control-flow с подграфом (см. вызывающий refresh).</param>
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
        string relatedGraphLayout,
        string controlFlowMainAxis,
        CodeNavigationMapSettings mapSettings,
        TraceSignals trace,
        CockpitSurfaceState? cockpitSurfaceCapturedOnUi)
    {
        var rows = new List<WorkspaceNavigationMapOrchestrator.RelatedRow>();
        var status = string.Empty;
        var anchorLabel = "—";
        CodeNavigationMapGraphSceneVm? scene = null;
        var graphPreferredHeight = CodeNavigationMapCompositor.DefaultHeightFile;
        var accentCount = 0;
        string? cfAnchorFullPath = null;
        string? wireError = null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out _))
            {
                status = WorkspaceNavigationMapOrchestrator.ResolveErrorStatus(root, currentPath);
            }
            else if (useSubgraphMode && GraphDocumentJson.TryParseRoot(root, out var subgraph, out wireError))
            {
                if (string.Equals(normalizedLevel, CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal)
                    && SolutionTreePath.TryGetFullPath(subgraph!.AnchorPath, out var anchorNorm))
                    cfAnchorFullPath = anchorNorm;

                var viewport = new SkiaInstrumentViewport(graphWidth, graphHeight);
                var composed = deps.MapCompositor.Compose(
                    new CodeNavigationMapCompositionIntent(
                        subgraph!,
                        normalizedLevel,
                        mapDetailLevel,
                        relatedGraphLayout,
                        controlFlowMainAxis),
                    viewport);
                if (normalizedLevel == CodeNavigationMapLevelKind.ControlFlow)
                {
                    ArgumentNullException.ThrowIfNull(cockpitSurfaceCapturedOnUi);
                    var channelPayload = deps.TraceFlowCoordinator.Build(new TraceFlowChannelContext(
                        subgraph!,
                        trace.ImpactedTestsBadge,
                        trace.LastTestSummary ?? ""));
                    var cdsDecision = deps.TraceFlowCdsRouter.Route(
                        new TraceFlowCdsRouteInput(cockpitSurfaceCapturedOnUi, normalizedLevel));
                    var baseScene = composed.ToSceneVm(graphWidth, composed.PreferredHeight, mapSettings, solutionPath);
                    scene = deps.TraceFlowSurfaceCompositor.Compose(baseScene, channelPayload, cdsDecision);
                }
                else
                {
                    scene = composed.ToSceneVm(graphWidth, composed.PreferredHeight, mapSettings, solutionPath);
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
            else if (useSubgraphMode && !string.IsNullOrEmpty(wireError))
            {
                status = FormatWireParseStatus(wireError, root);
            }

            return new DryResult(status, anchorLabel, scene, graphPreferredHeight, accentCount, rows, cfAnchorFullPath);
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DryResult(
                FormatComposeExceptionStatus(ex),
                "—",
                null,
                CodeNavigationMapCompositor.DefaultHeightFile,
                0,
                [],
                null);
        }
    }

    private static string FormatWireParseStatus(string wireError, JsonElement root)
    {
        if (wireError is "error" or "bad_mode" or "no_anchor" or "no_items")
        {
            var msg = root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String
                ? m.GetString()
                : null;
            if (!string.IsNullOrEmpty(msg))
                return msg;
        }

        return wireError switch
        {
            "no_items" => "Нет связанных файлов по текущим эвристикам.",
            "no_anchor" => "Не задан якорный файл для карты.",
            "bad_mode" => "Неподдерживаемый режим ответа навигации.",
            _ => $"Не удалось разобрать ответ навигации ({wireError})."
        };
    }

    private static string FormatComposeExceptionStatus(Exception ex) =>
        string.IsNullOrWhiteSpace(ex.Message)
            ? "Не удалось разобрать ответ навигации."
            : $"Не удалось разобрать ответ навигации: {ex.Message}";
}
