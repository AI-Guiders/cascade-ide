#nullable enable
using System.Text.Json;

namespace CascadeIDE.Cockpit.Graph;

/// <summary>Парсинг wire JSON режима <c>subgraph</c> / <c>related</c> в <see cref="GraphDocument"/> (ADR 0067).</summary>
public static class GraphDocumentJson
{
    public static bool TryParse(string json, out GraphDocument? doc, out string? error)
    {
        doc = null;
        error = null;
        try
        {
            using var j = JsonDocument.Parse(json);
            return TryParseRoot(j.RootElement, out doc, out error);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>Subgraph или related (для уровня file при graph/both).</summary>
    public static bool TryParseRoot(JsonElement root, out GraphDocument? doc, out string? error)
    {
        doc = null;
        error = null;
        if (root.TryGetProperty("error", out var errEl))
        {
            error = errEl.GetString() ?? "error";
            return false;
        }

        if (!root.TryGetProperty("mode", out var modeEl) || modeEl.ValueKind != JsonValueKind.String)
        {
            error = "bad_mode";
            return false;
        }

        return modeEl.GetString() switch
        {
            "subgraph" => TryParseSubgraphRoot(root, out doc, out error),
            "related" => TryParseRelatedRoot(root, out doc, out error),
            _ => Fail(out doc, out error, "bad_mode")
        };
    }

    private static bool TryParseSubgraphRoot(JsonElement root, out GraphDocument? doc, out string? error)
    {
        doc = null;
        error = null;

        var anchor = root.TryGetProperty("anchor_path", out var ap) && ap.ValueKind == JsonValueKind.String
            ? ap.GetString() ?? ""
            : "";
        if (string.IsNullOrEmpty(anchor))
            return Fail(out doc, out error, "no_anchor");

        var graphKind = TryParseGraphKind(root);
        var nodes = ParseSubgraphNodes(root);
        var edges = ParseSubgraphEdges(root);

        doc = new GraphDocument
        {
            AnchorPath = anchor,
            Kind = graphKind,
            Nodes = nodes,
            Edges = edges
        };
        return true;
    }

    private static bool TryParseRelatedRoot(JsonElement root, out GraphDocument? doc, out string? error)
    {
        doc = null;
        error = null;

        var anchor = root.TryGetProperty("anchor_path", out var ap) && ap.ValueKind == JsonValueKind.String
            ? ap.GetString() ?? ""
            : "";
        if (string.IsNullOrEmpty(anchor))
            return Fail(out doc, out error, "no_anchor");

        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return Fail(out doc, out error, "no_items");

        var nodes = new List<GraphNode>
        {
            new()
            {
                Id = "n0",
                Path = anchor,
                Kind = "anchor",
                Label = Path.GetFileName(anchor)
            }
        };
        var edges = new List<GraphEdge>();
        var idByPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [anchor] = "n0" };
        var n = 1;

        foreach (var el in items.EnumerateArray())
        {
            var path = el.TryGetProperty("path", out var pEl) ? pEl.GetString() : null;
            if (string.IsNullOrEmpty(path))
                continue;

            if (idByPath.ContainsKey(path))
                continue;

            var id = $"n{n++}";
            idByPath[path] = id;
            var relatedKind = el.TryGetProperty("kind", out var kindEl) ? kindEl.GetString() : null;
            var semanticKind = string.IsNullOrEmpty(relatedKind) ? "related" : relatedKind;
            var rel = el.TryGetProperty("relative_path", out var rEl) ? rEl.GetString() : null;
            var rat = el.TryGetProperty("rationale", out var raEl) ? raEl.GetString() : null;
            nodes.Add(new GraphNode
            {
                Id = id,
                Path = path,
                Kind = semanticKind,
                Label = Path.GetFileName(path),
                RelativePath = rel,
                Rationale = rat
            });
            edges.Add(new GraphEdge
            {
                FromId = "n0",
                ToId = id,
                Kind = "related_to",
                RelationKind = relatedKind
            });
        }

        doc = new GraphDocument
        {
            AnchorPath = anchor,
            Kind = GraphKind.RelatedFiles,
            Nodes = nodes,
            Edges = edges
        };
        return true;
    }

    private static List<GraphNode> ParseSubgraphNodes(JsonElement root)
    {
        var nodes = new List<GraphNode>();
        if (!root.TryGetProperty("nodes", out var nodesEl) || nodesEl.ValueKind != JsonValueKind.Array)
            return nodes;

        foreach (var el in nodesEl.EnumerateArray())
        {
            var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            var path = el.TryGetProperty("path", out var pEl) ? pEl.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(path))
                continue;
            var kind = el.TryGetProperty("kind", out var kEl) ? kEl.GetString() ?? "" : "";
            var label = el.TryGetProperty("label", out var lEl) ? lEl.GetString() ?? Path.GetFileName(path) : Path.GetFileName(path);
            var rel = el.TryGetProperty("relative_path", out var rEl) ? rEl.GetString() : null;
            var rat = el.TryGetProperty("rationale", out var raEl) ? raEl.GetString() : null;
            int? legendIndex = null;
            if (el.TryGetProperty("legend_index", out var liEl) && liEl.ValueKind == JsonValueKind.Number
                && liEl.TryGetInt32(out var li))
                legendIndex = li;
            var legendText = el.TryGetProperty("legend_text", out var ltEl) ? ltEl.GetString() : null;
            int? lineStart = null;
            int? lineEnd = null;
            if (el.TryGetProperty("line_start", out var lsEl) && lsEl.ValueKind == JsonValueKind.Number
                && lsEl.TryGetInt32(out var ls))
                lineStart = ls;
            if (el.TryGetProperty("line_end", out var leEl) && leEl.ValueKind == JsonValueKind.Number
                && leEl.TryGetInt32(out var le))
                lineEnd = le;
            int? loopGroupId = null;
            if (el.TryGetProperty("loop_group", out var lgEl) && lgEl.ValueKind == JsonValueKind.Number
                && lgEl.TryGetInt32(out var lg))
                loopGroupId = lg;
            nodes.Add(new GraphNode
            {
                Id = id,
                Path = path,
                Kind = kind,
                Label = string.IsNullOrEmpty(label) ? Path.GetFileName(path) : label,
                RelativePath = rel,
                Rationale = rat,
                LegendIndex = legendIndex,
                LegendText = legendText,
                LineStart = lineStart,
                LineEnd = lineEnd,
                LoopGroupId = loopGroupId
            });
        }

        return nodes;
    }

    private static List<GraphEdge> ParseSubgraphEdges(JsonElement root)
    {
        var edges = new List<GraphEdge>();
        if (!root.TryGetProperty("edges", out var edgesEl) || edgesEl.ValueKind != JsonValueKind.Array)
            return edges;

        foreach (var el in edgesEl.EnumerateArray())
        {
            var from = el.TryGetProperty("from_id", out var fEl) ? fEl.GetString() ?? "" : "";
            var to = el.TryGetProperty("to_id", out var tEl) ? tEl.GetString() ?? "" : "";
            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                continue;
            var k = el.TryGetProperty("kind", out var ke) ? ke.GetString() : null;
            var relationKind = el.TryGetProperty("relation_kind", out var rkeNew) && rkeNew.ValueKind == JsonValueKind.String
                ? rkeNew.GetString()
                : el.TryGetProperty("related_kind", out var rke) ? rke.GetString() : null;
            var provenance = el.TryGetProperty("edge_provenance", out var pe) && pe.ValueKind == JsonValueKind.String
                ? pe.GetString()
                : null;
            edges.Add(new GraphEdge
            {
                FromId = from,
                ToId = to,
                Kind = k,
                RelationKind = relationKind,
                EdgeProvenance = provenance
            });
        }

        return edges;
    }

    private static bool Fail(out GraphDocument? doc, out string? error, string code)
    {
        doc = null;
        error = code;
        return false;
    }

    public static GraphKind TryParseGraphKind(JsonElement root)
    {
        if (!root.TryGetProperty("graph_kind", out var g) || g.ValueKind != JsonValueKind.String)
            return GraphKind.Unspecified;
        var s = g.GetString();
        if (string.IsNullOrEmpty(s))
            return GraphKind.Unspecified;
        if (string.Equals(s, GraphKindWire.CodeIntent, StringComparison.Ordinal)
            || string.Equals(s, GraphKindWire.CodeIntentLegacy, StringComparison.Ordinal))
            return GraphKind.CodeIntent;
        if (string.Equals(s, GraphKindWire.RelatedFiles, StringComparison.Ordinal))
            return GraphKind.RelatedFiles;
        if (string.Equals(s, GraphKindWire.RepositoryModuleTree, StringComparison.Ordinal))
            return GraphKind.RepositoryModuleTree;
        return GraphKind.Unspecified;
    }
}
