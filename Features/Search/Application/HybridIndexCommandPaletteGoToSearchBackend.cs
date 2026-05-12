#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Search.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Workspace-поиск палитры через Hybrid Codebase Index (FTS; semantic выключен). ADR 0112 §7.</summary>
internal sealed class HybridIndexCommandPaletteGoToSearchBackend(
    HybridIndexOrchestrator orchestrator,
    string hybridScopeMode) : ICommandPaletteGoToSearchBackend
{
    public async Task<(IReadOnlyList<RipgrepWorkspaceMatch> Matches, string? Error)> SearchMatchesAsync(
        GoToAllQuery query,
        string workspaceRoot,
        string? solutionPath,
        int maxMatches,
        string? _rgExecutable,
        CancellationToken cancellationToken)
    {
        var surface = CommandPaletteHciQueryExtensions.TryBuildFtsSurfaceQuery(query);
        if (surface is null)
            return ([], null);

        var root = CanonicalFilePath.Normalize(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        (var hciRoot, var hciSolution) = HybridIndexScopeResolver.ApplyScopeMode(hybridScopeMode, root, solutionPath);
        var extensions = CommandPaletteHciQueryExtensions.FtsIncludeExtensions(query);
        var (response, searchErr) = await orchestrator
            .SearchHybridAsync(
                hciRoot,
                hciSolution,
                surface,
                topN: Math.Max(1, maxMatches),
                pathPrefix: null,
                excludePathPrefixes: null,
                extensions,
                semantic: false,
                alpha: 0.65,
                beta: 0.35,
                vecTopK: 30,
                cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrEmpty(searchErr))
            return ([], searchErr);

        return (CommandPaletteHybridIndexHitMapper.MapHits(response.Hits, hciRoot), null);
    }
}
