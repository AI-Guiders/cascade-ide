#nullable enable

namespace CascadeIDE.Cockpit.Graph;

/// <summary>
/// Изменяемый чертёж <see cref="GraphDocument"/> для разных источников (control flow, related files, GitMap).
/// Общий механический API graph-backed layer (ADR 0115).
/// </summary>
public class GraphDocumentBlueprint
{
    private int _nextId = 1;
    private int _legendSerial;

    public GraphDocumentBlueprint(
        string anchorPath,
        int maxNodes,
        int maxEdges,
        string anchorLabel,
        string anchorRationale,
        GraphKind graphKind = GraphKind.Unspecified)
    {
        AnchorPath = anchorPath;
        Kind = graphKind;
        MaxNodes = maxNodes;
        MaxEdges = maxEdges;
        Nodes.Add(new GraphBuildNode(
            AnchorNodeId,
            anchorPath,
            "anchor",
            anchorLabel,
            "",
            anchorRationale,
            null,
            null,
            null,
            null,
            null));
    }

    public string AnchorPath { get; }

    public string AnchorNodeId { get; } = "n0";

    public GraphKind Kind { get; }

    public int MaxNodes { get; }

    public int MaxEdges { get; }

    public List<GraphBuildNode> Nodes { get; } = [];

    public List<GraphBuildEdge> Edges { get; } = [];

    public bool TruncatedNodes { get; private set; }

    public bool TruncatedEdges { get; private set; }

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

    public string? TryAddNode(
        string kind,
        string nodePath,
        string label,
        string relativePath,
        string rationale,
        string? legendLine,
        bool assignControlFlowLegendIndex,
        int? lineStart = null,
        int? lineEnd = null,
        int? loopGroupId = null)
    {
        if (Nodes.Count >= MaxNodes)
        {
            TruncatedNodes = true;
            return null;
        }

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

        Nodes.Add(new GraphBuildNode(
            id, nodePath, kind, label, relativePath, rationale, legendIndex, legendText, lineStart, lineEnd, loopGroupId));
        return id;
    }

    public bool TryAddEdge(string fromId, string toId, string kind, string relationKind, string? edgeProvenance = null)
    {
        if (Edges.Count >= MaxEdges)
        {
            TruncatedEdges = true;
            return false;
        }
        Edges.Add(new GraphBuildEdge(fromId, toId, kind, relationKind, edgeProvenance));
        return true;
    }

    public void AddEdges(IReadOnlyList<string> fromIds, string toId, string kind, string relationKind, string? edgeProvenance = null)
    {
        if (fromIds.Count == 0)
            return;

        var edgeKind = fromIds.Count > 1 ? "Merge" : kind;
        foreach (var fromId in fromIds)
        {
            if (Edges.Count >= MaxEdges)
            {
                TruncatedEdges = true;
                break;
            }
            Edges.Add(new GraphBuildEdge(fromId, toId, edgeKind, relationKind, edgeProvenance));
        }
    }

    public string? TryAddSubmoduleRepository(string absolutePath, string shortLabel, string? rationale = null) =>
        TryAddNode(
            "submodule",
            absolutePath,
            shortLabel,
            "",
            rationale ?? "submodule",
            null,
            assignControlFlowLegendIndex: false);

    public bool TryLinkParentContainsSubmodule(string parentId, string childId, string edgeKind = "contains") =>
        TryAddEdge(parentId, childId, edgeKind, edgeKind);

    public GraphDocument ToDocument() =>
        new()
        {
            AnchorPath = AnchorPath,
            Kind = Kind,
            Nodes = Nodes.Select(n => new GraphNode
            {
                Id = n.Id,
                Path = n.Path,
                Kind = n.Kind,
                Label = n.Label,
                RelativePath = string.IsNullOrEmpty(n.RelativePath) ? null : n.RelativePath,
                Rationale = n.Rationale,
                LegendIndex = n.LegendIndex,
                LegendText = n.LegendText,
                LineStart = n.LineStart,
                LineEnd = n.LineEnd,
                LoopGroupId = n.LoopGroupId
            }).ToList(),
            Edges = Edges.Select(e => new GraphEdge
            {
                FromId = e.FromId,
                ToId = e.ToId,
                Kind = e.Kind,
                RelationKind = e.RelationKind,
                EdgeProvenance = e.EdgeProvenance
            }).ToList()
        };
}

public readonly record struct GraphBuildNode(
    string Id,
    string Path,
    string Kind,
    string Label,
    string RelativePath,
    string Rationale,
    int? LegendIndex,
    string? LegendText,
    int? LineStart,
    int? LineEnd,
    int? LoopGroupId);

public readonly record struct GraphBuildEdge(
    string FromId,
    string ToId,
    string Kind,
    string RelationKind,
    string? EdgeProvenance);
