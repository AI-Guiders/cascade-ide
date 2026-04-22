#nullable enable

namespace CascadeIDE.Services;

/// <summary>
/// Общий изменяемый чертёж subgraph (узлы/рёбра в том же смысле, что <see cref="CodeNavigationMapSubgraphDocument"/> и JSON MCP).
/// Один механический API для разных <b>источников</b>: control flow и связи кода (CodeNavigation), связанные файлы (WorkspaceNavigation), submodule-дерево (GitMap, ADR 0062) — без дублирования капов и Merge-логики рёбер.
/// </summary>
public sealed class CodeNavigationMapSubgraphBlueprint
{
    private int _nextId = 1;
    private int _legendSerial;

    public CodeNavigationMapSubgraphBlueprint(
        string anchorPath,
        int maxNodes,
        int maxEdges,
        string anchorLabel,
        string anchorRationale,
        CodeNavigationMapGraphKind graphKind = CodeNavigationMapGraphKind.Unspecified)
    {
        AnchorPath = anchorPath;
        GraphKind = graphKind;
        MaxNodes = maxNodes;
        MaxEdges = maxEdges;
        Nodes.Add(new SubgraphBuildNode(
            AnchorNodeId,
            anchorPath,
            "anchor",
            anchorLabel,
            "",
            anchorRationale,
            null,
            null));
    }

    /// <summary>Якорь subgraph (как правило файл control flow или корень worktree GitMap).</summary>
    public string AnchorPath { get; }

    public string AnchorNodeId { get; } = "n0";

    /// <summary>Тип графа для <see cref="ToDocument"/> и JSON MCP (<c>graph_kind</c>).</summary>
    public CodeNavigationMapGraphKind GraphKind { get; }

    public int MaxNodes { get; }

    public int MaxEdges { get; }

    public List<SubgraphBuildNode> Nodes { get; } = [];

    public List<SubgraphBuildEdge> Edges { get; } = [];

    /// <summary>Санитизация строки для легенды / подписи (общая для control flow и прочих сценариев).</summary>
    public static string SanitizeLegendLine(string? text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        var s = text.Replace('\r', ' ').Replace('\n', ' ');
        while (s.Contains("  ", StringComparison.Ordinal))
            s = s.Replace("  ", " ", StringComparison.Ordinal);
        s = s.Trim();
        if (s.Length <= maxLen)
            return s;
        return s[..(maxLen - 1)] + "…";
    }

    /// <summary>
    /// Узел с опциональной нумерацией легенды (control flow: <paramref name="assignControlFlowLegendIndex"/> = true).
    /// GitMap / прочие графы обычно передают false.
    /// </summary>
    public string? TryAddNode(
        string kind,
        string nodePath,
        string label,
        string relativePath,
        string rationale,
        string? legendLine,
        bool assignControlFlowLegendIndex)
    {
        if (Nodes.Count >= MaxNodes)
            return null;

        var id = $"n{_nextId++}";
        int? legendIndex = null;
        string? legendText = null;
        if (assignControlFlowLegendIndex)
        {
            var leg = string.IsNullOrWhiteSpace(legendLine)
                ? SanitizeLegendLine(rationale, 200)
                : SanitizeLegendLine(legendLine, 200);
            _legendSerial++;
            legendIndex = _legendSerial;
            legendText = string.IsNullOrEmpty(leg) ? null : leg;
        }

        Nodes.Add(new SubgraphBuildNode(id, nodePath, kind, label, relativePath, rationale, legendIndex, legendText));
        return id;
    }

    /// <summary>Ребро parent → child с одинаковым kind/related_kind (GitMap: «содержит»).</summary>
    public bool TryAddEdge(string fromId, string toId, string kind, string relatedKind)
    {
        if (Edges.Count >= MaxEdges)
            return false;
        Edges.Add(new SubgraphBuildEdge(fromId, toId, kind, relatedKind));
        return true;
    }

    /// <summary>
    /// Несколько источников → один приёмник; при нескольких источниках kind рёбра становится <c>Merge</c> (как в control flow).
    /// </summary>
    public void AddEdges(IReadOnlyList<string> fromIds, string toId, string kind, string relatedKind)
    {
        if (fromIds.Count == 0)
            return;

        var edgeKind = fromIds.Count > 1 ? "Merge" : kind;
        foreach (var fromId in fromIds)
        {
            if (Edges.Count >= MaxEdges)
                break;
            Edges.Add(new SubgraphBuildEdge(fromId, toId, edgeKind, relatedKind));
        }
    }

    #region Высокоуровневые операции (GitMap, ADR 0062)

    /// <summary>Узел корня submodule (путь каталога checkout).</summary>
    public string? TryAddSubmoduleRepository(string absolutePath, string shortLabel, string? rationale = null) =>
        TryAddNode(
            "submodule",
            absolutePath,
            shortLabel,
            "",
            rationale ?? "submodule",
            null,
            assignControlFlowLegendIndex: false);

    /// <summary>Ребро «родительский репозиторий содержит вложенный модуль».</summary>
    public bool TryLinkParentContainsSubmodule(string parentId, string childId, string edgeKind = "contains") =>
        TryAddEdge(parentId, childId, edgeKind, edgeKind);

    #endregion

    /// <summary>Снимок для композитора карты намерений и разбора subgraph без повторного копирования полей.</summary>
    public CodeNavigationMapSubgraphDocument ToDocument() =>
        new()
        {
            AnchorPath = AnchorPath,
            GraphKind = GraphKind,
            Nodes = Nodes.Select(n => new CodeNavigationMapSubgraphNode
            {
                Id = n.Id,
                Path = n.Path,
                Kind = n.Kind,
                Label = n.Label,
                RelativePath = string.IsNullOrEmpty(n.RelativePath) ? null : n.RelativePath,
                Rationale = n.Rationale,
                LegendIndex = n.LegendIndex,
                LegendText = n.LegendText
            }).ToList(),
            Edges = Edges.Select(e => new CodeNavigationMapSubgraphEdge
            {
                FromId = e.FromId,
                ToId = e.ToId,
                Kind = e.Kind,
                RelatedKind = e.RelatedKind
            }).ToList()
        };
}

public readonly record struct SubgraphBuildNode(
    string Id,
    string Path,
    string Kind,
    string Label,
    string RelativePath,
    string Rationale,
    int? LegendIndex,
    string? LegendText);

public readonly record struct SubgraphBuildEdge(
    string FromId,
    string ToId,
    string Kind,
    string RelatedKind);
