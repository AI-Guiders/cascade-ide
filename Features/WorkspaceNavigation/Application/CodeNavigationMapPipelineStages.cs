#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.SkiaInstruments;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

public readonly record struct CodeNavigationMapPipelineContext(
    GraphDocument Subgraph,
    string MapLevel,
    SkiaInstrumentViewport Viewport,
    CodeNavigationMapDetailLevel DetailLevel = CodeNavigationMapDetailLevel.Normal,
    string RelatedGraphLayout = CodeNavigationMapRelatedGraphLayoutKind.Radial,
    string ControlFlowMainAxis = CodeNavigationMapControlFlowMainAxisKind.Auto);

public readonly record struct CodeNavigationMapPipelineState(
    GraphDocument Subgraph,
    string MapLevel,
    SkiaInstrumentViewport Viewport,
    CodeNavigationMapDetailLevel DetailLevel,
    string RelatedGraphLayout,
    string NormalizedControlFlowMainAxis,
    GraphControlFlowMainAxis? ControlFlowMainAxisOverride,
    int EstimatedLevelCount,
    int LoopEdgeCount,
    int MultiBranchEdgeCount,
    bool IsDense);

public interface ICodeNavigationMapIntentStage
{
    CodeNavigationMapPipelineState Resolve(in CodeNavigationMapPipelineContext context);
}

public interface ICodeNavigationMapDeclutterStage
{
    CodeNavigationMapPipelineState Apply(in CodeNavigationMapPipelineState state);
}

public interface ICodeNavigationMapLayoutStage
{
    CodeNavigationMapCompositionResult Layout(in CodeNavigationMapPipelineState state);
}

public sealed class CodeNavigationMapIntentStage : ICodeNavigationMapIntentStage
{
    public CodeNavigationMapPipelineState Resolve(in CodeNavigationMapPipelineContext context)
    {
        var level = CodeNavigationMapLevelKind.Normalize(context.MapLevel);
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
        var cfAxis = CodeNavigationMapControlFlowMainAxisKind.Normalize(context.ControlFlowMainAxis);
        return new CodeNavigationMapPipelineState(
            doc,
            level,
            context.Viewport,
            context.DetailLevel,
            CodeNavigationMapRelatedGraphLayoutKind.Normalize(context.RelatedGraphLayout),
            cfAxis,
            GraphControlFlowLayoutMetrics.TryControlFlowMainAxisOverride(cfAxis),
            estimatedLevelCount,
            loopEdgeCount,
            multiBranchEdgeCount,
            isDense);
    }

    private static int EstimateLevelCount(GraphDocument doc)
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

public sealed class CodeNavigationMapDeclutterStage(ICodeNavigationMapIntentStage intentStage) : ICodeNavigationMapDeclutterStage
{
    private readonly ICodeNavigationMapIntentStage _intentStage = intentStage;

    public CodeNavigationMapPipelineState Apply(in CodeNavigationMapPipelineState state)
    {
        var filtered = CodeNavigationMapControlFlowDeclutter.TryTransform(state);
        if (filtered is null)
            return state;

        var ctx = new CodeNavigationMapPipelineContext(
            filtered,
            state.MapLevel,
            state.Viewport,
            state.DetailLevel,
            state.RelatedGraphLayout,
            state.NormalizedControlFlowMainAxis);
        return _intentStage.Resolve(ctx);
    }
}

public sealed class CodeNavigationMapLayoutStage(
    Func<string, IGraphLayoutEngine>? fileLayoutResolver = null,
    IGraphLayoutEngine? controlFlowLayout = null) : ICodeNavigationMapLayoutStage
{
    private readonly Func<string, IGraphLayoutEngine> _fileLayoutResolver =
        fileLayoutResolver ?? ResolveRelatedFileLayout;
    private readonly IGraphLayoutEngine _controlFlowLayout = controlFlowLayout ?? new ControlFlowGraphLayoutEngine();

    public CodeNavigationMapCompositionResult Layout(in CodeNavigationMapPipelineState state)
    {
        var viewport = state.Viewport;
        var width = viewport.Width > 0 ? viewport.Width : CodeNavigationMapCompositor.DefaultWidth;
        var presentation = GraphLayoutPresentationResolver.Resolve(state.Subgraph, state.MapLevel);
        var isControlFlow = string.Equals(state.MapLevel, CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal);

        if (!isControlFlow)
        {
            var satelliteCount = Math.Max(0, state.Subgraph.Nodes.Count - 1);
            var intrinsicHeight = GraphFileLayoutMetrics.EstimatePreferredHeight(
                satelliteCount,
                state.DetailLevel,
                state.RelatedGraphLayout);
            var preferredHeight = viewport.Height > 0
                ? Math.Max(viewport.Height, intrinsicHeight)
                : intrinsicHeight;
            var fileLayout = ResolveAutoRelatedFileLayout(state.RelatedGraphLayout, satelliteCount, width, preferredHeight, state.DetailLevel)
                             ?? _fileLayoutResolver(state.RelatedGraphLayout);
            var layoutScene = fileLayout.Layout(state.Subgraph, width, preferredHeight, state.DetailLevel, controlFlowMainAxisOverride: null)
                .WithPresentation(presentation);
            return new CodeNavigationMapCompositionResult(
                layoutScene,
                preferredHeight,
                Array.Empty<CodeNavigationMapInstrumentBlockDescriptor>());
        }

        var estimatedLevels = state.EstimatedLevelCount;
        if (state.IsDense)
            estimatedLevels += 2;
        var computedHeight = GraphControlFlowLayoutMetrics.EstimatePreferredHeight(
            estimatedLevels,
            state.DetailLevel);
        var preferredCfHeight = Math.Clamp(
            computedHeight,
            GraphViewportMetrics.DefaultHeightControlFlow,
            GraphViewportMetrics.MaxHeightControlFlow);

        var cfScene = _controlFlowLayout.Layout(
                state.Subgraph,
                width,
                preferredCfHeight,
                state.DetailLevel,
                state.ControlFlowMainAxisOverride,
                new GraphLayoutEngineOptions(state.IsDense))
            .WithPresentation(presentation);
        return new CodeNavigationMapCompositionResult(
            cfScene,
            preferredCfHeight,
            Array.Empty<CodeNavigationMapInstrumentBlockDescriptor>());
    }

    private static IGraphLayoutEngine ResolveRelatedFileLayout(string? relatedLayout) =>
        CodeNavigationMapRelatedGraphLayoutKind.Normalize(relatedLayout) switch
        {
            CodeNavigationMapRelatedGraphLayoutKind.TopDown => new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: true),
            CodeNavigationMapRelatedGraphLayoutKind.BottomUp => new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: false),
            _ => new StarGraphLayoutEngine()
        };

    private static IGraphLayoutEngine? ResolveAutoRelatedFileLayout(
        string relatedLayout,
        int satelliteCount,
        double width,
        double height,
        CodeNavigationMapDetailLevel detailLevel)
    {
        if (CodeNavigationMapRelatedGraphLayoutKind.Normalize(relatedLayout) != CodeNavigationMapRelatedGraphLayoutKind.Auto)
            return null;

        // Heuristic: hierarchy is great when it has enough vertical space; otherwise radial is more readable.
        if (satelliteCount <= 6)
            return new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: true);

        var desiredHierarchy = GraphFileLayoutMetrics.EstimateHierarchyHeight(satelliteCount, detailLevel);
        var cramped = height < desiredHierarchy * 0.70 || height < 180 || width < 220;
        return cramped ? new StarGraphLayoutEngine() : new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: true);
    }
}
