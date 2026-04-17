#nullable enable
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Services.Navigation;

/// <summary>Уровень детализации композиции Semantic Map.</summary>
public enum SemanticMapDetailLevel
{
    Glance,
    Normal,
    Inspect
}

/// <summary>Контекст запроса на композицию Semantic Map.</summary>
public readonly record struct SemanticMapPipelineContext(
    WorkspaceNavigationSubgraphDocument Subgraph,
    string SemanticMapLevel,
    SkiaInstrumentViewport Viewport,
    SemanticMapDetailLevel DetailLevel = SemanticMapDetailLevel.Normal);

/// <summary>Промежуточное состояние pipeline Semantic Map.</summary>
public readonly record struct SemanticMapPipelineState(
    WorkspaceNavigationSubgraphDocument Subgraph,
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

    private static int EstimateLevelCount(WorkspaceNavigationSubgraphDocument doc)
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
/// Declutter stage v1: пока без агрессивной фильтрации. Слой выделен отдельно для будущих профилей glance/inspect.
/// </summary>
public sealed class SemanticMapDeclutterStage : ISemanticMapDeclutterStage
{
    public SemanticMapPipelineState Apply(in SemanticMapPipelineState state)
    {
        // V1: структура графа не меняется. Слой нужен как отдельная граница политики шума.
        return state;
    }
}

/// <summary>Layout stage v1: выбирает движок и рассчитывает рекомендуемую высоту viewport.</summary>
public sealed class SemanticMapLayoutStage(
    IWorkspaceNavigationGraphLayoutEngine? fileLayout = null,
    IWorkspaceNavigationGraphLayoutEngine? controlFlowLayout = null) : ISemanticMapLayoutStage
{
    private readonly IWorkspaceNavigationGraphLayoutEngine _fileLayout = fileLayout ?? new WorkspaceNavigationStarGraphLayoutEngine();
    private readonly IWorkspaceNavigationGraphLayoutEngine _controlFlowLayout = controlFlowLayout ?? new WorkspaceNavigationControlFlowGraphLayoutEngine();

    public SemanticMapCompositionResult Layout(in SemanticMapPipelineState state)
    {
        var viewport = state.Viewport;
        var width = viewport.Width > 0 ? viewport.Width : SemanticMapCompositor.DefaultWidth;
        var isControlFlow = string.Equals(state.SemanticMapLevel, Models.SemanticMapLevelKind.ControlFlow, StringComparison.Ordinal);

        if (!isControlFlow)
        {
            var preferredHeight = viewport.Height > 0 ? viewport.Height : SemanticMapCompositor.DefaultHeightFile;
            var scene = _fileLayout.Layout(state.Subgraph, width, preferredHeight);
            return new SemanticMapCompositionResult(scene, preferredHeight);
        }

        var spacingPerLevel = state.DetailLevel switch
        {
            SemanticMapDetailLevel.Glance => 28,
            SemanticMapDetailLevel.Inspect => 40,
            _ => 34
        };
        var computedHeight = 42 + Math.Max(1, state.EstimatedLevelCount) * spacingPerLevel;
        var preferredCfHeight = Math.Clamp(
            viewport.Height > 0 ? Math.Max(viewport.Height, computedHeight) : computedHeight,
            SemanticMapCompositor.DefaultHeightControlFlow,
            420);

        var cfScene = _controlFlowLayout.Layout(state.Subgraph, width, preferredCfHeight);
        return new SemanticMapCompositionResult(cfScene, preferredCfHeight);
    }
}
