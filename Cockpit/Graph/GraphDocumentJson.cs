#nullable enable
using System.Text.Json;

namespace CascadeIDE.Cockpit.Graph;

/// <summary>Парсинг wire JSON режима <c>subgraph</c> в <see cref="GraphDocument"/> (ADR 0067).</summary>
public static class GraphDocumentJson
{
    public static bool TryParse(string json, out GraphDocument? doc, out string? error)
    {
        doc = null;
        error = null;
        try
        {
            using var j = JsonDocument.Parse(json);
            var root = j.RootElement;
            if (root.TryGetProperty("error", out var errEl))
            {
                error = errEl.GetString() ?? "error";
                return false;
            }

            if (!root.TryGetProperty("mode", out var modeEl) || modeEl.GetString() != "subgraph")
            {
                error = "not_subgraph";
                return false;
            }

            var anchor = root.TryGetProperty("anchor_path", out var ap) && ap.ValueKind == JsonValueKind.String
                ? ap.GetString() ?? ""
                : "";
            if (string.IsNullOrEmpty(anchor))
            {
                error = "no_anchor";
                return false;
            }

            var graphKind = TryParseGraphKind(root);

            var nodes = new List<GraphNode>();
            if (root.TryGetProperty("nodes", out var nodesEl) && nodesEl.ValueKind == JsonValueKind.Array)
            {
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
                    nodes.Add(new GraphNode
                    {
                        Id = id,
                        Path = path,
                        Kind = kind,
                        Label = string.IsNullOrEmpty(label) ? Path.GetFileName(path) : label,
                        RelativePath = rel,
                        Rationale = rat,
                        LegendIndex = legendIndex,
                        LegendText = legendText
                    });
                }
            }

            var edges = new List<GraphEdge>();
            if (root.TryGetProperty("edges", out var edgesEl) && edgesEl.ValueKind == JsonValueKind.Array)
            {
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
            }

            doc = new GraphDocument
            {
                AnchorPath = anchor,
                Kind = graphKind,
                Nodes = nodes,
                Edges = edges
            };
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
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
