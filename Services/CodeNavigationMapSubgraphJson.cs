#nullable enable
using System.IO;
using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Парсинг JSON режима <c>subgraph</c> без дублирования логики построения графа.</summary>
public static class CodeNavigationMapSubgraphJson
{
    public static bool TryParse(string json, out CodeNavigationMapSubgraphDocument? doc, out string? error)
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

            var nodes = new List<CodeNavigationMapSubgraphNode>();
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
                    nodes.Add(new CodeNavigationMapSubgraphNode
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

            var edges = new List<CodeNavigationMapSubgraphEdge>();
            if (root.TryGetProperty("edges", out var edgesEl) && edgesEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in edgesEl.EnumerateArray())
                {
                    var from = el.TryGetProperty("from_id", out var fEl) ? fEl.GetString() ?? "" : "";
                    var to = el.TryGetProperty("to_id", out var tEl) ? tEl.GetString() ?? "" : "";
                    if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
                        continue;
                    var k = el.TryGetProperty("kind", out var ke) ? ke.GetString() : null;
                    var rk = el.TryGetProperty("related_kind", out var rke) ? rke.GetString() : null;
                    edges.Add(new CodeNavigationMapSubgraphEdge { FromId = from, ToId = to, Kind = k, RelatedKind = rk });
                }
            }

            doc = new CodeNavigationMapSubgraphDocument
            {
                AnchorPath = anchor,
                GraphKind = graphKind,
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

    private static CodeNavigationMapGraphKind TryParseGraphKind(JsonElement root)
    {
        if (!root.TryGetProperty("graph_kind", out var g) || g.ValueKind != JsonValueKind.String)
            return CodeNavigationMapGraphKind.Unspecified;
        var s = g.GetString();
        if (string.IsNullOrEmpty(s))
            return CodeNavigationMapGraphKind.Unspecified;
        if (string.Equals(s, CodeNavigationMapGraphKindWire.CodeIntent, StringComparison.Ordinal)
            || string.Equals(s, CodeNavigationMapGraphKindWire.CodeIntentLegacy, StringComparison.Ordinal))
            return CodeNavigationMapGraphKind.CodeIntent;
        if (string.Equals(s, CodeNavigationMapGraphKindWire.RelatedFiles, StringComparison.Ordinal))
            return CodeNavigationMapGraphKind.RelatedFiles;
        if (string.Equals(s, CodeNavigationMapGraphKindWire.RepositoryModuleTree, StringComparison.Ordinal))
            return CodeNavigationMapGraphKind.RepositoryModuleTree;
        return CodeNavigationMapGraphKind.Unspecified;
    }
}
