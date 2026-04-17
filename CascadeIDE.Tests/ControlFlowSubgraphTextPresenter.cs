using System.Text;
using System.Text.Json;

namespace CascadeIDE.Tests;

internal static class ControlFlowSubgraphTextPresenter
{
    public static string Render(string subgraphJson)
    {
        using var doc = JsonDocument.Parse(subgraphJson);
        var root = doc.RootElement;
        var nodeElements = root.GetProperty("nodes").EnumerateArray().ToList();
        var nodes = nodeElements
            .ToDictionary(
                n => n.GetProperty("id").GetString() ?? string.Empty,
                ToCaption,
                StringComparer.Ordinal);
        var edges = root.GetProperty("edges").EnumerateArray()
            .Select((e, index) => new EdgeView(
                e.GetProperty("from_id").GetString() ?? string.Empty,
                e.GetProperty("to_id").GetString() ?? string.Empty,
                e.GetProperty("kind").GetString() ?? "Call",
                index))
            .ToList();
        if (edges.Count == 0)
            return string.Join(", ", nodes.Values);

        var anchorId = nodeElements
            .Where(n => string.Equals(n.GetProperty("kind").GetString(), "anchor", StringComparison.Ordinal))
            .Select(n => n.GetProperty("id").GetString())
            .FirstOrDefault()
            ?? "n0";

        var outgoing = edges
            .GroupBy(e => e.FromId, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Order).ToList(),
                StringComparer.Ordinal);

        var hasBranch = outgoing.Values.Any(v => v.Count > 1);
        return hasBranch
            ? RenderBranched(anchorId, nodes, outgoing)
            : RenderLinear(anchorId, nodes, outgoing, edges.Count);
    }

    private static string RenderLinear(
        string anchorId,
        IReadOnlyDictionary<string, string> nodes,
        IReadOnlyDictionary<string, List<EdgeView>> outgoing,
        int edgeCount)
    {
        var sb = new StringBuilder();
        var currentId = anchorId;
        sb.Append(nodes.GetValueOrDefault(currentId, currentId));

        // edgeCount bound protects from accidental cycles.
        for (var i = 0; i < edgeCount; i++)
        {
            if (!outgoing.TryGetValue(currentId, out var nextEdges) || nextEdges.Count == 0)
                break;

            var edge = nextEdges[0];
            sb.Append(" -(").Append(edge.Kind).Append(")-> ")
                .Append(nodes.GetValueOrDefault(edge.ToId, edge.ToId));
            currentId = edge.ToId;
        }

        return sb.ToString();
    }

    private static string RenderBranched(
        string anchorId,
        IReadOnlyDictionary<string, string> nodes,
        IReadOnlyDictionary<string, List<EdgeView>> outgoing)
    {
        var sb = new StringBuilder();
        sb.Append(nodes.GetValueOrDefault(anchorId, anchorId));
        AppendBranches(sb, anchorId, "", nodes, outgoing, []);
        return sb.ToString();
    }

    private static void AppendBranches(
        StringBuilder sb,
        string fromId,
        string prefix,
        IReadOnlyDictionary<string, string> nodes,
        IReadOnlyDictionary<string, List<EdgeView>> outgoing,
        HashSet<string> path)
    {
        if (!outgoing.TryGetValue(fromId, out var edges) || edges.Count == 0)
            return;

        var localPath = new HashSet<string>(path, StringComparer.Ordinal) { fromId };
        for (var i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            var isLast = i == edges.Count - 1;
            var branch = isLast ? "`-- " : "|-- ";
            var nextPrefix = prefix + (isLast ? "    " : "|   ");
            var toCaption = nodes.GetValueOrDefault(edge.ToId, edge.ToId);
            var isCycle = localPath.Contains(edge.ToId);

            sb.AppendLine();
            sb.Append(prefix)
                .Append(branch)
                .Append('(').Append(edge.Kind).Append(") ")
                .Append(toCaption);
            if (isCycle)
            {
                sb.Append(" [cycle]");
                continue;
            }

            AppendBranches(sb, edge.ToId, nextPrefix, nodes, outgoing, localPath);
        }
    }

    private static string ToCaption(JsonElement node)
    {
        var kind = node.GetProperty("kind").GetString() ?? string.Empty;
        return kind switch
        {
            "anchor" => "A",
            "condition_step" => "?",
            "exit_step" => "R",
            _ => node.GetProperty("label").GetString() ?? "?"
        };
    }

    private readonly record struct EdgeView(string FromId, string ToId, string Kind, int Order);
}
