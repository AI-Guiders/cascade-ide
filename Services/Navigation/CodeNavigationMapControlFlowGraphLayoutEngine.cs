#nullable enable
using Avalonia;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>
/// Укладка control-flow в формате "полётного плана": основной поток сверху вниз,
/// а узлы одного шага по глубине — в сторону от центральной оси.
/// </summary>
public sealed class CodeNavigationMapControlFlowGraphLayoutEngine : ICodeNavigationMapSubgraphLayoutEngine
{
    public CodeNavigationMapGraphSceneVm Layout(CodeNavigationMapSubgraphDocument doc, double width, double height)
    {
        if (width <= 0 || height <= 0)
            return new CodeNavigationMapGraphSceneVm
            {
                Nodes = [],
                Edges = [],
                Legend = [],
                LegendColumnLeft = width,
                LegendPlacement = CodeNavigationMapLegendBlockPlacement.BesideGraph,
                LegendBlockTopY = 0
            };

        var anchor = doc.Nodes.FirstOrDefault(n => string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                     ?? doc.Nodes.FirstOrDefault(n => n.Id.Equals("n0", StringComparison.OrdinalIgnoreCase))
                     ?? (doc.Nodes.Count > 0 ? doc.Nodes[0] : null);
        if (anchor is null)
            return new CodeNavigationMapGraphSceneVm
            {
                Nodes = [],
                Edges = [],
                Legend = [],
                LegendColumnLeft = width,
                LegendPlacement = CodeNavigationMapLegendBlockPlacement.BesideGraph,
                LegendBlockTopY = 0
            };

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

        var topPadding = CodeNavigationMapGraphPrimitives.ControlFlowTopPadding;
        var bottomPadding = CodeNavigationMapGraphPrimitives.ControlFlowBottomPadding;
        var sidePadding = CodeNavigationMapGraphPrimitives.ControlFlowSidePadding;
        var legendGap = CodeNavigationMapGraphPrimitives.ControlFlowLegendGap;

        static bool IsExitStep(CodeNavigationMapSubgraphNode n) =>
            string.Equals(n.Kind, "exit_step", StringComparison.OrdinalIgnoreCase);

        static bool IsConditionStep(CodeNavigationMapSubgraphNode n) =>
            string.Equals(n.Kind, "condition_step", StringComparison.OrdinalIgnoreCase);

        var hasLegendRows = doc.Nodes.Any(static n =>
            !IsExitStep(n)
            && n.LegendIndex is > 0
            && !string.IsNullOrWhiteSpace(n.LegendText));
        var showLegendConditionKey = doc.Nodes.Any(IsConditionStep);
        var showLegendReturnKey = doc.Nodes.Any(IsExitStep);
        var showLegendExceptionFlowKey = doc.Nodes.Any(static n =>
            string.Equals(n.Kind, "handler_step", StringComparison.OrdinalIgnoreCase));
        ComputeEdgeStyleLegend(
            doc.Edges,
            out var showLegendEdgeStyleKey,
            out var edgeStyleLegendRowCount);
        var useLegendColumn = hasLegendRows || showLegendConditionKey || showLegendReturnKey
            || showLegendExceptionFlowKey || showLegendEdgeStyleKey;
        var legendRowsPreview = hasLegendRows
            ? doc.Nodes
                .Where(n =>
                    !IsExitStep(n)
                    && n.LegendIndex is > 0
                    && !string.IsNullOrWhiteSpace(n.LegendText))
                .OrderBy(n => n.LegendIndex.GetValueOrDefault())
                .Select(n => new CodeNavigationMapLegendEntry { Index = n.LegendIndex!.Value, Text = n.LegendText!.Trim() })
                .ToList()
            : new List<CodeNavigationMapLegendEntry>();

        var contentLegendNeed = EstimateLegendColumnContentWidth(
            legendRowsPreview,
            showLegendReturnKey,
            showLegendConditionKey,
            showLegendExceptionFlowKey,
            showLegendEdgeStyleKey);
        var fallbackFraction = width * CodeNavigationMapGraphPrimitives.ControlFlowLegendReserveWidthFraction;
        var legendReserveCap = CodeNavigationMapGraphPrimitives.ResolveControlFlowLegendReserveCap(width);
        var legendReserveLo = Math.Min(CodeNavigationMapGraphPrimitives.ControlFlowLegendReserveMin, legendReserveCap);
        var firstLegendReserve = useLegendColumn
            ? Math.Clamp(
                Math.Max(fallbackFraction, contentLegendNeed),
                legendReserveLo,
                legendReserveCap)
            : 0;
        IReadOnlyList<CodeNavigationMapLegendEntry> legendRows = legendRowsPreview;

        CodeNavigationMapGraphSceneVm BuildFor(double heightForY, double legendResForWidth)
        {
        var graphWidth = Math.Max(
            CodeNavigationMapGraphPrimitives.ControlFlowMinGraphWidth,
            width - legendResForWidth - (useLegendColumn ? legendGap : 0));

        // Узлы на одном уровне не разъезжаются на всю ширину слота — ограниченная «полоса чтения», по центру области графа.
        var bandW = CodeNavigationMapGraphPrimitives.ResolveControlFlowReadableBandWidth(graphWidth);
        var bandLeft = (graphWidth - bandW) * 0.5;
        var centerX = bandLeft + bandW * 0.5;
        var labelCharBudget = CodeNavigationMapGraphPrimitives.ResolveControlFlowLabelCharBudget(bandW);

        var levelCount = Math.Max(1, levels.Count);
        var innerH = heightForY - topPadding - bottomPadding;
        var slotCount = Math.Max(1, levelCount - 1);
        var rawYStep = innerH / slotCount;
        var minYStep = CodeNavigationMapGraphPrimitives.MinVerticalStepForLevelCount(levelCount);
        var maxYStep = Math.Min(
            CodeNavigationMapGraphPrimitives.ControlFlowMaxReadableVerticalStepCap,
            Math.Max(CodeNavigationMapGraphPrimitives.ControlFlowMaxReadableVerticalStep, rawYStep));
        var yStep = Math.Clamp(rawYStep, minYStep, maxYStep);
        var verticalSpan = Math.Max(0, levelCount - 1) * yStep;
        var yStart = topPadding + (innerH - verticalSpan) * 0.5;
        var radiusMul = Math.Clamp(
            yStep / CodeNavigationMapGraphPrimitives.ControlFlowRefVerticalStep,
            CodeNavigationMapGraphPrimitives.ControlFlowRadiusScaleMin,
            CodeNavigationMapGraphPrimitives.ControlFlowRadiusScaleMax);
        var horizontalRadiusScale = Math.Clamp(
            bandW / CodeNavigationMapGraphPrimitives.ControlFlowMaxReadableBandWidth,
            CodeNavigationMapGraphPrimitives.ControlFlowHorizontalRadiusScaleMin,
            1.0);
        radiusMul *= horizontalRadiusScale;
        var sideLabelFontPx = CodeNavigationMapGraphPrimitives.ResolveControlFlowSideLabelFontSize(bandW, yStep);
        var anchorR = CodeNavigationMapGraphPrimitives.ControlFlowAnchorRadiusBase * radiusMul;
        var nodeR = CodeNavigationMapGraphPrimitives.ControlFlowNodeRadiusBase * radiusMul;

        var idToCenter = new Dictionary<string, Point>(StringComparer.OrdinalIgnoreCase);
        var idToRadius = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var nodeLayouts = new List<CodeNavigationMapGraphNodeLayout>(doc.Nodes.Count);

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
                    ? CodeNavigationMapNodeShape.Condition
                    : CodeNavigationMapNodeShape.Circle;
                nodeLayouts.Add(new CodeNavigationMapGraphNodeLayout
                {
                    Id = n.Id,
                    Kind = n.Kind,
                    FullPath = n.Path,
                    Label = TruncateLabel(n.Label, labelCharBudget),
                    Center = point,
                    Radius = radius,
                    IsAnchor = isAnchor,
                    Shape = shape,
                    LegendIndex = n.LegendIndex,
                    LegendLine = n.LegendText
                });
            }
        }

        var edgeLayouts = new List<CodeNavigationMapGraphEdgeLayout>(doc.Edges.Count);
        foreach (var e in doc.Edges)
        {
            if (!idToCenter.TryGetValue(e.FromId, out var from))
                continue;
            if (!idToCenter.TryGetValue(e.ToId, out var to))
                continue;

            edgeLayouts.Add(new CodeNavigationMapGraphEdgeLayout
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

        // Колонка легенды: сразу справа от фактического «чернильного» правого края узлов, а не от правой границы
        // полосы чтения (bandW может быть 380px при одном столбце узлов — тогда зазор до текста нелепо большой).
        var legendColumnLeft = width;
        if (useLegendColumn)
        {
            if (nodeLayouts.Count > 0)
            {
                var inkSlack = CodeNavigationMapGraphPrimitives.ControlFlowBesideLegendInkSlack;
                var minClear = Math.Max(legendGap, CodeNavigationMapGraphPrimitives.ControlFlowLegendBesideMinClearance);
                var maxInkRight = 0.0;
                foreach (var n in nodeLayouts)
                    maxInkRight = Math.Max(maxInkRight, n.Center.X + n.Radius + inkSlack);
                legendColumnLeft = maxInkRight + minClear;
            }
            else
            {
                var minClear = Math.Max(legendGap, CodeNavigationMapGraphPrimitives.ControlFlowLegendBesideMinClearance);
                legendColumnLeft = bandLeft + bandW + minClear;
            }
        }

        return new CodeNavigationMapGraphSceneVm
        {
            Nodes = nodeLayouts,
            Edges = edgeLayouts,
            Legend = legendRows,
            UseLegendColumn = useLegendColumn,
            ShowLegendConditionKey = showLegendConditionKey,
            ShowLegendReturnKey = showLegendReturnKey,
            ShowLegendExceptionFlowKey = showLegendExceptionFlowKey,
            ShowLegendEdgeStyleKey = showLegendEdgeStyleKey,
            LegendColumnLeft = legendColumnLeft,
            LegendPlacement = CodeNavigationMapLegendBlockPlacement.BesideGraph,
            LegendBlockTopY = 0,
            SideLabelFontSizePx = sideLabelFontPx
        };
        }

        var sceneBeside = BuildFor(height, firstLegendReserve);
        if (!useLegendColumn)
            return sceneBeside;

        var textRoomBeside = width - sceneBeside.LegendColumnLeft - 4;
        // Колонка у правого края вьюпорта — соседняя невозможна без наложения на граф/обрезки.
        var besideUnusable = sceneBeside.LegendColumnLeft + 8 >= width;
        var needBelow = besideUnusable
            || textRoomBeside < CodeNavigationMapGraphPrimitives.ControlFlowLegendSideColumnMinTextWidth
            || contentLegendNeed > textRoomBeside + 0.5;
        if (!needBelow)
            return sceneBeside;

        var hasShapeKeyRows = showLegendReturnKey || showLegendConditionKey || showLegendExceptionFlowKey;
        var capEst = sceneBeside.SideLabelFontSizePx ?? 11.0;
        var estimatedLegendH = CodeNavigationMapGraphPrimitives.EstimateControlFlowLegendBlockHeight(
            legendRows.Count,
            hasShapeKeyRows,
            edgeStyleLegendRowCount,
            capEst);
        var belowGap = CodeNavigationMapGraphPrimitives.ControlFlowLegendBelowBlockGap;
        var graphH = height - estimatedLegendH - belowGap;
        if (graphH < CodeNavigationMapGraphPrimitives.ControlFlowMinGraphHeightForBelowLegend)
            return sceneBeside;

        var sceneBelow = BuildFor(graphH, 0);
        if (sceneBelow.Nodes.Count == 0)
            return sceneBeside;

        var maxBottom = 0.0;
        foreach (var n in sceneBelow.Nodes)
            maxBottom = Math.Max(maxBottom, n.Center.Y + n.Radius);
        var legendTopY = maxBottom + belowGap;
        if (legendTopY + estimatedLegendH > height + 1.0)
            return sceneBeside;

        return new CodeNavigationMapGraphSceneVm
        {
            Nodes = sceneBelow.Nodes,
            Edges = sceneBelow.Edges,
            Legend = sceneBelow.Legend,
            UseLegendColumn = useLegendColumn,
            ShowLegendConditionKey = showLegendConditionKey,
            ShowLegendReturnKey = showLegendReturnKey,
            ShowLegendExceptionFlowKey = showLegendExceptionFlowKey,
            ShowLegendEdgeStyleKey = showLegendEdgeStyleKey,
            LegendColumnLeft = sidePadding,
            LegendPlacement = CodeNavigationMapLegendBlockPlacement.BelowGraph,
            LegendBlockTopY = legendTopY,
            SideLabelFontSizePx = sceneBelow.SideLabelFontSizePx
        };
    }

    /// <summary>Оценка минимальной ширины колонки легенды (индекс + текст + блок ключей фигур).</summary>
    private static double EstimateLegendColumnContentWidth(
        IReadOnlyList<CodeNavigationMapLegendEntry> rows,
        bool showReturnKey,
        bool showConditionKey,
        bool showExceptionKey,
        bool showEdgeStyleKey = false)
    {
        const double charPx = 6.15;
        const double idxPad = 36;
        const double margin = 20;
        var maxLine = 0;
        foreach (var row in rows)
            maxLine = Math.Max(maxLine, Math.Min(row.Text.Length, 220));

        var body = idxPad + maxLine * charPx;
        var keys = 0d;
        if (showReturnKey || showConditionKey || showExceptionKey)
            keys = idxPad + 96;
        if (showEdgeStyleKey)
            keys = Math.Max(keys, idxPad + 220);

        return Math.Max(body, keys) + margin;
    }

    /// <summary>Блок «стили рёбер» в легенде: только если в графе есть нестандартные рёбра (пунктир, цикл).</summary>
    private static void ComputeEdgeStyleLegend(
        IReadOnlyList<CodeNavigationMapSubgraphEdge> edges,
        out bool show,
        out int rowCount)
    {
        static bool KindContains(string? kind, string needle) =>
            !string.IsNullOrEmpty(kind) && kind.Contains(needle, StringComparison.OrdinalIgnoreCase);

        var needsNonSolidLegend = edges.Any(e =>
            KindContains(e.Kind, "conditional")
            || KindContains(e.Kind, "exception")
            || KindContains(e.Kind, "multibranch")
            || KindContains(e.Kind, "loop"));
        if (!needsNonSolidLegend)
        {
            show = false;
            rowCount = 0;
            return;
        }

        show = true;
        rowCount = 1; // сплошная — контраст с пунктиром
        if (edges.Any(e => KindContains(e.Kind, "conditional") || KindContains(e.Kind, "exception")))
            rowCount++;
        if (edges.Any(e => KindContains(e.Kind, "multibranch")))
            rowCount++;
        if (edges.Any(e => KindContains(e.Kind, "loop")))
            rowCount++;
    }

    private static Dictionary<string, List<string>> BuildOutgoing(IReadOnlyList<CodeNavigationMapSubgraphEdge> edges)
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

    private static string TruncateLabel(string label, int maxChars)
    {
        if (label.Length <= maxChars)
            return label;
        var take = Math.Max(1, maxChars - 1);
        return label[..take] + "…";
    }
}
