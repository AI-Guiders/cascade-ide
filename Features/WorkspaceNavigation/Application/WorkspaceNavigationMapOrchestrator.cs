#nullable enable
using System.Text.Json;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Contracts;
using CascadeIDE.Features.Documents;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.CodeNavigation;

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

    /// <summary>
    /// CF refresh: только позиция каретки в текущем открытом <c>.cs</c>; без подстановки «первого метода» файла.
    /// </summary>
    public static (int? line, int? column) ResolveControlFlowCursorForRefresh(
        string? navigationPath,
        string? currentPath,
        string? sourceText,
        int? caretOrSelectionOffset,
        int? navigateToLine = null,
        int? navigateToColumn = null)
    {
        if (string.IsNullOrWhiteSpace(currentPath)
            || !currentPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return (null, null);

        if (!EditorTextCoordinateUtilities.PathsReferToSameFile(navigationPath ?? "", currentPath))
            return (null, null);

        if (string.IsNullOrEmpty(sourceText))
            return (null, null);

        return IdeMcpNavigationOrchestrator.ResolveControlFlowLineColumn(
            navigateToLine,
            navigateToColumn,
            sourceText,
            caretOrSelectionOffset);
    }

    /// <summary>1-based line → offset в <paramref name="text"/> (для якоря клика по узлу CF).</summary>
    public static int? TryOffsetForLine(string? text, int lineOneBased, int columnOneBased = 1)
    {
        if (string.IsNullOrEmpty(text) || lineOneBased < 1)
            return null;

        var line = 1;
        var i = 0;
        while (line < lineOneBased && i < text.Length)
        {
            if (text[i++] == '\n')
                line++;
        }

        if (line != lineOneBased)
            return null;

        var col = Math.Max(1, columnOneBased) - 1;
        while (col > 0 && i < text.Length && text[i] != '\n')
        {
            i++;
            col--;
        }

        return Math.Clamp(i, 0, text.Length);
    }

    /// <summary>
    /// Путь для JSON подграфа карты: CF — всегда активный <c>.cs</c> редактора, если он задан (без замены на Program.cs /
    /// «первый открытый» из дерева решения); иначе эвристический якорь.
    /// </summary>
    public static string? ResolveNavigationPathForGraphJson(
        string normalizedLevel,
        string? currentPath,
        string? anchorPath,
        IReadOnlyList<string> _)
    {
        var fallback = anchorPath ?? currentPath;
        if (!string.Equals(normalizedLevel, CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal))
            return fallback;

        if (string.IsNullOrWhiteSpace(currentPath)
            || !currentPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return fallback;

        return SolutionTreePath.TryGetFullPath(currentPath, out var full) ? full : currentPath;
    }

    public static string ResolveErrorStatus(JsonElement root, string? currentPath)
    {
        if (!root.TryGetProperty("error", out var errEl))
            return string.Empty;

        var code = errEl.GetString() ?? string.Empty;
        var msg = root.TryGetProperty("message", out var mEl) ? mEl.GetString() ?? string.Empty : string.Empty;
        if (code == "no_file" && string.IsNullOrEmpty(currentPath))
            return "Откройте файл из дерева решения — здесь появятся связанные.";
        if (code == "no_control_flow_scope")
            return string.IsNullOrEmpty(msg)
                ? "Поставьте курсор в метод или top-level оператор."
                : msg;
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
