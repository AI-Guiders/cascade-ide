#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>Multi-hop related-files graph for Glass SemanticMap Skia (WNM JSON deferred).</summary>
public static class GlassSemanticMapGraph
{
    public sealed record Node(string FilePath, string Kind, string Rationale, int Hop)
    {
        public string Display =>
            string.IsNullOrWhiteSpace(Rationale)
                ? $"h{Hop} · {Kind} · {Path.GetFileName(FilePath)}"
                : $"h{Hop} · {Kind} · {Path.GetFileName(FilePath)} · {Rationale}";
    }

    public sealed record Edge(string FromPath, string ToPath, string Reason);

    public sealed record Graph(
        string? FocusPath,
        IReadOnlyList<Node> Nodes,
        IReadOnlyList<Edge> Edges);

    public static Graph Collect(string? workspaceRoot, string? editorPath, int maxNodes = 64, int maxHop = 2)
    {
        if (maxNodes < 1)
            return new Graph(editorPath, [], []);

        var focus = string.IsNullOrWhiteSpace(editorPath) ? null : editorPath;
        var nodes = new List<Node>();
        var edges = new List<Edge>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddNode(GlassRelatedFilesFeed.Item item, int hop)
        {
            if (nodes.Count >= maxNodes || !seen.Add(item.FullPath))
                return;
            nodes.Add(new Node(item.FullPath, item.Kind, item.Rationale, hop));
        }

        void AddEdge(string from, string to, string reason)
        {
            if (edges.Count >= maxNodes * 2)
                return;
            edges.Add(new Edge(from, to, reason));
        }

        if (focus is null || !File.Exists(focus))
            return new Graph(focus, nodes, edges);

        var hop0 = GlassRelatedFilesFeed.Collect(workspaceRoot, focus, max: maxNodes);
        foreach (var item in hop0)
        {
            AddNode(item, 1);
            AddEdge(focus, item.FullPath, item.Rationale);
        }

        if (maxHop < 2)
            return new Graph(focus, nodes, edges);

        var frontier = hop0.Take(Math.Min(hop0.Count, 12)).ToList();
        foreach (var parent in frontier)
        {
            if (nodes.Count >= maxNodes)
                break;

            var hop1 = GlassRelatedFilesFeed.Collect(workspaceRoot, parent.FullPath, max: 16);
            foreach (var child in hop1)
            {
                if (string.Equals(child.FullPath, focus, StringComparison.OrdinalIgnoreCase))
                    continue;
                AddNode(child, 2);
                AddEdge(parent.FullPath, child.FullPath, child.Rationale);
                if (nodes.Count >= maxNodes)
                    break;
            }
        }

        return new Graph(focus, nodes, edges);
    }
}
