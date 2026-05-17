#nullable enable
using System.Text.Json;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Application-level helpers for workspace navigation map refresh.
/// Keeps status/row shaping and caret position normalization out of MainWindowViewModel.
/// </summary>
[ApplicationOrchestrator]
public static class WorkspaceNavigationMapOrchestrator
{
    public sealed record RelatedRow(string FullPath, string RelativePath, string Kind, string Rationale);

    public static (int line, int column) ComputeLineColumn(string? text, int? offset)
    {
        var source = text ?? string.Empty;
        var pos = Math.Clamp(offset ?? 0, 0, source.Length);
        var line = 1;
        var col = 1;
        for (var i = 0; i < pos; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                col = 1;
            }
            else
            {
                col++;
            }
        }

        return (line, col);
    }

    public static string ResolveErrorStatus(JsonElement root, string? currentPath)
    {
        if (!root.TryGetProperty("error", out var errEl))
            return string.Empty;

        var code = errEl.GetString() ?? string.Empty;
        var msg = root.TryGetProperty("message", out var mEl) ? mEl.GetString() ?? string.Empty : string.Empty;
        if (code == "no_file" && string.IsNullOrEmpty(currentPath))
            return "Откройте файл из дерева решения — здесь появятся связанные.";
        return string.IsNullOrEmpty(msg) ? code : msg;
    }

    public static List<RelatedRow> BuildRowsFromSubgraph(GraphDocument subgraph, string? solutionPath)
    {
        var rows = new List<RelatedRow>();
        foreach (var n in subgraph.Nodes)
        {
            if (string.Equals(n.Kind, "anchor", StringComparison.OrdinalIgnoreCase))
                continue;

            var rel = string.IsNullOrEmpty(n.RelativePath)
                ? McpSolutionTree.GetRelativePath(solutionPath, n.Path)
                : n.RelativePath!;

            rows.Add(new RelatedRow(
                n.Path,
                rel ?? n.Path,
                n.Kind,
                n.Rationale ?? string.Empty));
        }

        return rows;
    }

    public static string ResolveAnchorLabelFromSubgraph(GraphDocument subgraph) =>
        string.IsNullOrEmpty(subgraph.AnchorPath)
            ? "—"
            : Path.GetFileName(subgraph.AnchorPath);

    public static string ResolveAnchorLabelFromRelatedRoot(JsonElement root)
    {
        if (root.TryGetProperty("anchor_path", out var ap) && ap.ValueKind == JsonValueKind.String)
        {
            var apStr = ap.GetString();
            if (!string.IsNullOrEmpty(apStr))
                return Path.GetFileName(apStr);
        }

        return "—";
    }

    public static List<RelatedRow> BuildRowsFromRelatedRoot(JsonElement root)
    {
        var rows = new List<RelatedRow>();
        if (!root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return rows;

        foreach (var el in items.EnumerateArray())
        {
            var fp = el.TryGetProperty("path", out var pEl) ? pEl.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrEmpty(fp))
                continue;

            var rel = el.TryGetProperty("relative_path", out var rEl) ? rEl.GetString() ?? string.Empty : string.Empty;
            var kind = el.TryGetProperty("kind", out var kEl) ? kEl.GetString() ?? string.Empty : string.Empty;
            var rationale = el.TryGetProperty("rationale", out var raEl) ? raEl.GetString() ?? string.Empty : string.Empty;
            rows.Add(new RelatedRow(
                fp,
                string.IsNullOrEmpty(rel) ? fp : rel,
                kind,
                rationale));
        }

        return rows;
    }

    public static string ResolveEmptyStatus(IReadOnlyCollection<RelatedRow> rows, string currentStatus, bool wantList) =>
        rows.Count == 0 && string.IsNullOrEmpty(currentStatus) && wantList
            ? "Нет связанных файлов по текущим эвристикам."
            : currentStatus;
}
