#nullable enable
using CascadeIDE.Features.Search.DataAcquisition;
using CascadeIDE.Features.Shell.Application;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Search.Application;

internal sealed class RipgrepCommandPaletteGoToSearchBackend : ICommandPaletteGoToSearchBackend
{
    public Task<(IReadOnlyList<RipgrepWorkspaceMatch> Matches, string? Error)> SearchMatchesAsync(
        GoToAllQuery query,
        string workspaceRoot,
        string? _solutionPath,
        int maxMatches,
        string? rgExecutable,
        CancellationToken cancellationToken)
    {
        var (pattern, fixedString, glob) = GoToPaletteRipgrepPatternBuilder.Build(query);
        return RipgrepWorkspaceSearchService.SearchMatchesAsync(
            workspaceRoot,
            pattern,
            subPath: null,
            fixedString,
            glob,
            maxMatches,
            rgExecutable,
            cancellationToken);
    }
}
