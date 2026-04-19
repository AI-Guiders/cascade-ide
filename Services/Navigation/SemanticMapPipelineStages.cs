#nullable enable
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Models;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Контекст запроса на композицию Semantic Map.</summary>
public readonly record struct SemanticMapPipelineContext(
    SemanticMapSubgraphDocument Subgraph,
    string SemanticMapLevel,
    SkiaInstrumentViewport Viewport,
    SemanticMapDetailLevel DetailLevel = SemanticMapDetailLevel.Normal);

/// <summary>Промежуточное состояние pipeline Semantic Map.</summary>
public readonly record struct SemanticMapPipelineState(
    SemanticMapSubgraphDocument Subgraph,
    string SemanticMapLevel,
    SkiaInstrumentViewport Viewport,
    SemanticMapDetailLevel DetailLevel,
    int EstimatedLevelCount,
    int LoopEdgeCount,
    int MultiBranchEdgeCount,
    bool IsDense);

public interface ISemanticMapIntentStage
{
    SemanticMapPipelineState Resolve(in SemanticMapPipelineContext context);
}

public interface ISemanticMapDeclutterStage
{
    SemanticMapPipelineState Apply(in SemanticMapPipelineState state);
}

public interface ISemanticMapLayoutStage
{
    SemanticMapCompositionResult Layout(in SemanticMapPipelineState state);
}

/// <summary>
/// Intent stage v1: нормализация уровня и сбор базовых метрик графа для последующих стадий.
/// </summary>
public sealed class SemanticMapIntentStage : ISemanticMapIntentStage
{
    public SemanticMapPipelineState Resolve(in SemanticMapPipelineContext context)
    {
        var level = Models.SemanticMapLevelKind.Normalize(context.SemanticMapLevel);
        var doc = context.Subgraph;
        var loopEdgeCount = 0;
        var multiBranchEdgeCount = 0;
        foreach (var e in doc.Edges)
        {
            if (!string.IsNullOrWhiteSpace(e.Kind) && e.Kind.Contains("loop", StringComparison.OrdinalIgnoreCase))
                loopEdgeCount++;
            if (!string.IsNullOrWhiteSpace(e.Kind) && e.Kind.Contains("multibranch", StringComparison.OrdinalIgnoreCase))
                multiBranchEdgeCount++;
        }

        var estimatedLevelCount = EstimateLevelCount(doc);
        var isDense = doc.Nodes.Count > 8 || estimatedLevelCount > 6;
        return new SemanticMapPipelineState(
            doc,
            level,
            context.Viewport,
            context.DetailLevel,
            estimatedLevelCount,
            loopEdgeCount,
            multiBranchEdgeCount,
            isDense);
    }

    private static int EstimateLevelCount(SemanticMapSubgraphDocument doc)
    {
        if (doc.Nodes.Count <= 1)
            return 1;

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes[0];
        var outgoing = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in doc.Edges)
        {
            if (!outgoing.TryGetValue(e.FromId, out var list))
            {
                list = [];
                outgoing[e.FromId] = list;
            }

            list.Add(e.ToId);
        }

        var depthById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { [anchor.Id] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(anchor.Id);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            var depth = depthById[id];
            if (!outgoing.TryGetValue(id, out var next))
                continue;

            foreach (var to in next)
            {
                if (depthById.ContainsKey(to))
                    continue;
                depthById[to] = depth + 1;
                queue.Enqueue(to);
            }
        }

        return Math.Max(1, depthById.Count == 0 ? 1 : depthById.Values.Max() + 1);
    }
}

/// <summary>
/// Declutter: политика «что показывать» до геометрии (ADR 0055). Glance + control flow: убираем второстепенные multibranch-связи.
/// </summary>
public sealed class SemanticMapDeclutterStage(ISemanticMapIntentStage intentStage) : ISemanticMapDeclutterStage
{
    private readonly ISemanticMapIntentStage _intentStage = intentStage;

    public SemanticMapPipelineState Apply(in SemanticMapPipelineState state)
    {
        var filtered = TryGlanceFilterControlFlow(state);
        if (filtered is null)
            return state;

        var ctx = new SemanticMapPipelineContext(
            filtered,
            state.SemanticMapLevel,
            state.Viewport,
            state.DetailLevel);
        return _intentStage.Resolve(ctx);
    }

    /// <summary>Glance: исключаем рёбра multibranch и недостижимые от anchor узлы; метрики пересчитываются через Intent.</summary>
    private static SemanticMapSubgraphDocument? TryGlanceFilterControlFlow(in SemanticMapPipelineState state)
    {
        if (state.DetailLevel != SemanticMapDetailLevel.Glance)
            return null;
        if (!string.Equals(state.SemanticMapLevel, SemanticMapLevelKind.ControlFlow, StringComparison.Ordinal))
            return null;

        var doc = state.Subgraph;
        var edgesWithoutMulti = doc.Edges.Where(e => !IsMultibranchEdge(e)).ToList();
        if (edgesWithoutMulti.Count == doc.Edges.Count)
            return null;

        var anchorId = FindAnchorNodeId(doc);
        var reachable = ReachableForward(anchorId, edgesWithoutMulti);
        var nodes = doc.Nodes.Where(n => reachable.Contains(n.Id)).ToList();
        var kept = new HashSet<string>(reachable, StringComparer.OrdinalIgnoreCase);
        var finalEdges = edgesWithoutMulti.Where(e => kept.Contains(e.FromId) && kept.Contains(e.ToId)).ToList();

        return new SemanticMapSubgraphDocument
        {
            AnchorPath = doc.AnchorPath,
            GraphKind = doc.GraphKind,
            Nodes = nodes,
            Edges = finalEdges
        };
    }

    private static bool IsMultibranchEdge(SemanticMapSubgraphEdge e) =>
        !string.IsNullOrWhiteSpace(e.Kind)
        && e.Kind.Contains("multibranch", StringComparison.OrdinalIgnoreCase);

    private static string FindAnchorNodeId(SemanticMapSubgraphDocument doc)
    {
        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase));
        if (anchor is not null)
            return anchor.Id;
        var n0 = doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase));
        if (n0 is not null)
            return n0.Id;
        return doc.Nodes[0].Id;
    }

    private static HashSet<string> ReachableForward(string anchorId, IReadOnlyList<SemanticMapSubgraphEdge> edges)
    {
        var outgoing = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in edges)
        {
            if (!outgoing.TryGetValue(e.FromId, out var list))
            {
                list = [];
                outgoing[e.FromId] = list;
            }

            list.Add(e.ToId);
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(anchorId);
        seen.Add(anchorId);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!outgoing.TryGetValue(id, out var next))
                continue;
            foreach (var t in next)
            {
                if (seen.Add(t))
                    queue.Enqueue(t);
            }
        }

        return seen;
    }
}

/// <summary>Layout stage v1: выбирает движок и рассчитывает рекомендуемую высоту viewport.</summary>
public sealed class SemanticMapLayoutStage(
    ISemanticMapSubgraphLayoutEngine? fileLayout = null,
    ISemanticMapSubgraphLayoutEngine? controlFlowLayout = null) : ISemanticMapLayoutStage
{
    private readonly ISemanticMapSubgraphLayoutEngine _fileLayout = fileLayout ?? new SemanticMapStarGraphLayoutEngine();
    private readonly ISemanticMapSubgraphLayoutEngine _controlFlowLayout = controlFlowLayout ?? new SemanticMapControlFlowGraphLayoutEngine();

    public SemanticMapCompositionResult Layout(in SemanticMapPipelineState state)
    {
        var viewport = state.Viewport;
        var width = viewport.Width > 0 ? viewport.Width : SemanticMapCompositor.DefaultWidth;
        var isControlFlow = string.Equals(state.SemanticMapLevel, Models.SemanticMapLevelKind.ControlFlow, StringComparison.Ordinal);

        if (!isControlFlow)
        {
            var preferredHeight = viewport.Height > 0 ? viewport.Height : SemanticMapCompositor.DefaultHeightFile;
            var scene = _fileLayout.Layout(state.Subgraph, width, preferredHeight);
            scene = SemanticMapGraphSceneVm.WithPresentationKind(
                scene,
                SemanticMapPresentationResolver.Resolve(state.Subgraph, state.SemanticMapLevel));
            return new SemanticMapCompositionResult(scene, preferredHeight);
        }

        var computedHeight = SemanticMapGraphPrimitives.EstimateControlFlowPreferredHeight(
            state.EstimatedLevelCount,
            state.DetailLevel);
        // Высота панели от «читаемого» интринсика, не от растягивания под весь слот — иначе рёбра и шаг становятся нечитаемыми.
        var preferredCfHeight = Math.Clamp(
            computedHeight,
            SemanticMapGraphPrimitives.DefaultViewportHeightControlFlow,
            SemanticMapGraphPrimitives.MaxViewportHeightControlFlow);

        var cfScene = _controlFlowLayout.Layout(state.Subgraph, width, preferredCfHeight);
        var presented = SemanticMapGraphSceneVm.WithPresentationKind(
            cfScene,
            SemanticMapPresentationResolver.Resolve(state.Subgraph, state.SemanticMapLevel));
        return new SemanticMapCompositionResult(presented, preferredCfHeight);
    }
}
