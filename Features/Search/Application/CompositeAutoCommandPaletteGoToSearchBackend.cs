#nullable enable
using CascadeIDE.Contracts;
using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.Search.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Search.Application;

/// <summary><c>auto</c>: пробуем HCI при готовности индекса; при ошибке — ripgrep. ADR 0112 §7.</summary>
internal sealed class CompositeAutoCommandPaletteGoToSearchBackend(
    HybridIndexOrchestrator orchestrator,
    string hybridScopeMode,
    ICommandPaletteGoToSearchBackend ripgrep,
    HybridIndexCommandPaletteGoToSearchBackend hciExclusive) : ICommandPaletteGoToSearchBackend
{
    public async Task<(IReadOnlyList<RipgrepWorkspaceMatch> Matches, string? Error)> SearchMatchesAsync(
        GoToAllQuery query,
        string workspaceRoot,
        string? solutionPath,
        int maxMatches,
        string? rgExecutable,
        CancellationToken cancellationToken)
    {
        try
        {
            var rootNorm = CanonicalFilePath.Normalize(
                workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            (var hciRoot, var hciSolution) = HybridIndexScopeResolver.ApplyScopeMode(hybridScopeMode, rootNorm, solutionPath);
            var st = await orchestrator.GetIndexStatusAsync(hciRoot, hciSolution, cancellationToken).ConfigureAwait(false);
            if (!st.DatabaseExists || st.DocumentCount <= 0 || !string.IsNullOrEmpty(st.LastReindexError))
                return await ripgrep
                    .SearchMatchesAsync(query, workspaceRoot, solutionPath, maxMatches, rgExecutable, cancellationToken)
                    .ConfigureAwait(false);

            var (hciMatches, hciErr) = await hciExclusive
                .SearchMatchesAsync(query, workspaceRoot, solutionPath, maxMatches, rgExecutable, cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrEmpty(hciErr))
                return await ripgrep
                    .SearchMatchesAsync(query, workspaceRoot, solutionPath, maxMatches, rgExecutable, cancellationToken)
                    .ConfigureAwait(false);

            return (hciMatches, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return await ripgrep
                .SearchMatchesAsync(query, workspaceRoot, solutionPath, maxMatches, rgExecutable, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
