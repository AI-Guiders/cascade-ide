#nullable enable
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Models;
using HybridCodebaseIndex.Core;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>
/// Лёгкий FTS-запрос к HCI для строки ориентации у карты намерений (без semantic, малый topN).
/// </summary>
public static class SemanticMapHciOrientationAcquirer
{
    private const int DefaultTopN = 5;

    /// <summary>Возвращает null, если HCI выключен, нет workspace или нечего искать.</summary>
    public static async Task<SemanticMapHciOrientationSnapshot?> TryAcquireAsync(
        IHybridIndexOrchestratorSearch orchestrator,
        HybridIndexSettings hybridSettings,
        string workspaceRoot,
        string? solutionPath,
        string? currentFilePath,
        CancellationToken cancellationToken)
    {
        if (!hybridSettings.Enabled)
            return null;

        var root = (workspaceRoot ?? "").Trim();
        if (string.IsNullOrEmpty(root))
            return null;

        var query = BuildQueryFromCurrentPath(currentFilePath);
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var (hciRoot, hciSln) = HybridIndexScopeResolver.ApplyScopeMode(hybridSettings.ScopeMode, root, solutionPath);

        (SearchResponse response, string? err) = await orchestrator
            .SearchHybridAsync(
                hciRoot,
                hciSln,
                query,
                topN: DefaultTopN,
                pathPrefix: null,
                excludePathPrefixes: null,
                extensions: null,
                semantic: false,
                alpha: 0.65,
                beta: 0.35,
                vecTopK: 30,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(err))
            return new SemanticMapHciOrientationSnapshot([], query, err);

        if (response.Hits.Count == 0)
            return new SemanticMapHciOrientationSnapshot([], query, null);

        var hits = new List<SemanticMapHciOrientationHit>(response.Hits.Count);
        foreach (var h in response.Hits)
        {
            var leaf = Path.GetFileName((h.Path ?? "").Replace('/', Path.DirectorySeparatorChar));
            if (string.IsNullOrEmpty(leaf))
                leaf = h.Path ?? "—";
            var line = Math.Max(h.LineStart, 1);
            hits.Add(new SemanticMapHciOrientationHit(leaf, h.HitKind ?? "?", line, h.Snippet ?? ""));
        }

        return new SemanticMapHciOrientationSnapshot(hits, query, null);
    }

    internal static string? BuildQueryFromCurrentPath(string? currentFilePath)
    {
        if (string.IsNullOrWhiteSpace(currentFilePath))
            return null;
        try
        {
            var name = Path.GetFileName(currentFilePath.Trim());
            if (string.IsNullOrEmpty(name))
                return null;
            var noExt = Path.GetFileNameWithoutExtension(name);
            return string.IsNullOrWhiteSpace(noExt) ? name.Trim() : noExt.Trim();
        }
        catch
        {
            return null;
        }
    }
}
