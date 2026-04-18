#nullable enable
using Avalonia;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Укладка control-flow в формате "полётного плана": основной поток сверху вниз,
/// а узлы одного шага по глубине — в сторону от центральной оси.
/// </summary>
public sealed class SemanticMapControlFlowGraphLayoutEngine : ISemanticMapSubgraphLayoutEngine
{
    public SemanticMapGraphSceneVm Layout(SemanticMapSubgraphDocument doc, double width, double height)
    {
        if (width <= 0 || height <= 0)
            return new SemanticMapGraphSceneVm { Nodes = [], Edges = [], Legend = [], LegendColumnLeft = width };

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase))
                     ?? (doc.Nodes.Count > 0 ? doc.Nodes[0] : null);
        if (anchor is null)
            return new SemanticMapGraphSceneVm { Nodes = [], Edges = [], Legend = [], LegendColumnLeft = width };

        var nodeById = doc.Nodes.ToDictionary(n => n.Id, StringComparer.OrdinalIgnoreCase);
        var outgoing = BuildOutgoing(doc.Edges);
        var depthById = BuildDepthMap(anchor.Id, outgoing);

        // Узлы, недостижимые от anchor, ставим в хвост (последние уровни), чтобы не терялись.
        var maxDepth = depthById.Count == 0 ? 0 : depthById.Values.Max();
        foreach (var n in doc.Nodes)
        {
            if (depthById.ContainsKey(n.Id))
                continue;
            maxDepth++;
            depthById[n.Id] = maxDepth;
        }

        var levels = depthById
            .GroupBy(kv => kv.Value)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Select(kv => kv.Key).ToList());

        var topPadding = SemanticMapGraphPrimitives.ControlFlowTopPadding;
        var bottomPadding = SemanticMapGraphPrimitives.ControlFlowBottomPadding;
        var sidePadding = SemanticMapGraphPrimitives.ControlFlowSidePadding;
        var legendGap = SemanticMapGraphPrimitives.ControlFlowLegendGap;

        static bool IsExitStep(SemanticMapSubgraphNode n) =>
            string.Equals(n.Kind, "exit_step", StringComparison.OrdinalIgnoreCase);

        static bool IsConditionStep(SemanticMapSubgraphNode n) =>
            string.Equals(n.Kind, "condition_step", StringComparison.OrdinalIgnoreCase);

        var hasLegendRows = doc.Nodes.Any(static n =>
            !IsExitStep(n)
            && n.LegendIndex is > 0
            && !string.IsNullOrWhiteSpace(n.LegendText));
        var showLegendConditionKey = doc.Nodes.Any(IsConditionStep);
        var showLegendReturnKey = doc.Nodes.Any(IsExitStep);
        var useLegendColumn = hasLegendRows || showLegendConditionKey || showLegendReturnKey;
        // Резерв под колонку текста справа (минимум читаемой ширины для строк легенды).
        var legendReserve = useLegendColumn
            ? Math.Clamp(
                width * SemanticMapGraphPrimitives.ControlFlowLegendReserveWidthFraction,
                SemanticMapGraphPrimitives.ControlFlowLegendReserveMin,
                SemanticMapGraphPrimitives.ControlFlowLegendReserveMax)
            : 0;
        var graphWidth = Math.Max(
            SemanticMapGraphPrimitives.ControlFlowMinGraphWidth,
            width - legendReserve - (useLegendColumn ? legendGap : 0));

        var legendRows = hasLegendRows
            ? doc.Nodes
                .Where(n =>
                    !IsExitStep(n)
                    && n.LegendIndex is > 0
                    && !string.IsNullOrWhiteSpace(n.LegendText))
                .OrderBy(n => n.LegendIndex.GetValueOrDefault())
                .Select(n => new SemanticMapLegendEntry { Index = n.LegendIndex!.Value, Text = n.LegendText!.Trim() })
                .ToList()
            : (IReadOnlyList<SemanticMapLegendEntry>)[];

        // Узлы на одном уровне не разъезжаются на всю ширину слота — ограниченная «полоса чтения», по центру области графа.
        var bandW = Math.Min(graphWidth, SemanticMapGraphPrimitives.ControlFlowMaxReadableBandWidth);
        var bandLeft = (graphWidth - bandW) * 0.5;
        var centerX = bandLeft + bandW * 0.5;

        // Легенда сразу справа от нарисованного графа, а не от края всей левой половины (иначе огромный зазор).
        var legendColumnLeft = useLegendColumn ? bandLeft + bandW + legendGap : width;

        var levelCount = Math.Max(1, levels.Count);
        var innerH = height - topPadding - bottomPadding;
        var slotCount = Math.Max(1, levelCount - 1);
        var rawYStep = innerH / slotCount;
        var minYStep = SemanticMapGraphPrimitives.MinVerticalStepForLevelCount(levelCount);
        var yStep = Math.Clamp(
            rawYStep,
            minYStep,
            SemanticMapGraphPrimitives.ControlFlowMaxReadableVerticalStep);
        var verticalSpan = Math.Max(0, levelCount - 1) * yStep;
        var yStart = topPadding + (innerH - verticalSpan) * 0.5;
        var radiusMul = Math.Clamp(
            yStep / SemanticMapGraphPrimitives.ControlFlowRefVerticalStep,
            SemanticMapGraphPrimitives.ControlFlowRadiusScaleMin,
            SemanticMapGraphPrimitives.ControlFlowRadiusScaleMax);
        var anchorR = SemanticMapGraphPrimitives.ControlFlowAnchorRadiusBase * radiusMul;
        var nodeR = SemanticMapGraphPrimitives.ControlFlowNodeRadiusBase * radiusMul;

        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        var idToRadius = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var nodeLayouts = new List<SemanticMapGraphNodeLayout>(doc.Nodes.Count);

        foreach (var (depth, ids) in levels)
        {
            var y = yStart + depth * yStep;
            var orderedIds = ids
                .OrderBy(id => string.Equals(id, anchor.Id, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var count = orderedIds.Count;
            var minX = bandLeft + sidePadding + nodeR;
            var maxX = Math.Max(minX, bandLeft + bandW - sidePadding - nodeR);
            for (var i = 0; i < count; i++)
            {
                var id = orderedIds[i];
                if (!nodeById.TryGetValue(id, out var n))
                    continue;

                var x = count == 1
                    ? centerX
                    : minX + (maxX - minX) * i / (count - 1);
                var radius = string.Equals(id, anchor.Id, StringComparison.OrdinalIgnoreCase) ? anchorR : nodeR;
                var point = new Point(x, y);
                idToCenter[id] = point;
                idToRadius[id] = radius;
                var isAnchor = string.Equals(n.Id, anchor.Id, StringComparison.OrdinalIgnoreCase);
                var shape = !isAnchor && string.Equals(n.Kind, "condition_step", StringComparison.OrdinalIgnoreCase)
                    ? SemanticMapNodeShape.Condition
                    : SemanticMapNodeShape.Circle;
                nodeLayouts.Add(new SemanticMapGraphNodeLayout
                {
                    Id = n.Id,
                    Kind = n.Kind,
                    FullPath = n.Path,
                    Label = TruncateLabel(n.Label),
                    Center = point,
                    Radius = radius,
                    IsAnchor = isAnchor,
                    Shape = shape,
                    LegendIndex = n.LegendIndex,
                    LegendLine = n.LegendText
                });
            }
        }

        var edgeLayouts = new List<SemanticMapGraphEdgeLayout>(doc.Edges.Count);
        foreach (var e in doc.Edges)
        {
            if (!idToCenter.TryGetValue(e.FromId, out var from))
                continue;
            if (!idToCenter.TryGetValue(e.ToId, out var to))
                continue;

            edgeLayouts.Add(new SemanticMapGraphEdgeLayout
            {
                FromNodeId = e.FromId,
                ToNodeId = e.ToId,
                From = from,
                To = to,
                ToRadius = idToRadius.TryGetValue(e.ToId, out var toR) ? toR : nodeR,
                Kind = e.Kind,
                RelatedKind = e.RelatedKind
            });
        }

        return new SemanticMapGraphSceneVm
        {
            Nodes = nodeLayouts,
            Edges = edgeLayouts,
            Legend = legendRows,
            UseLegendColumn = useLegendColumn,
            ShowLegendConditionKey = showLegendConditionKey,
            ShowLegendReturnKey = showLegendReturnKey,
            LegendColumnLeft = legendColumnLeft
        };
    }

    private static Dictionary<string, List<string>> BuildOutgoing(IReadOnlyList<SemanticMapSubgraphEdge> edges)
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

        return outgoing;
    }

    private static Dictionary<string, int> BuildDepthMap(string anchorId, Dictionary<string, List<string>> outgoing)
    {
        var depthById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [anchorId] = 0
        };
        var queue = new Queue<string>();
        queue.Enqueue(anchorId);

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

        return depthById;
    }

    private static string TruncateLabel(string label)
    {
        if (label.Length <= SemanticMapGraphPrimitives.ControlFlowLabelMaxLength)
            return label;
        return label[..SemanticMapGraphPrimitives.ControlFlowLabelTruncateLength] + "…";
    }
}
