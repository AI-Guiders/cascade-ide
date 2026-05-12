#nullable enable
using CascadeIDE.Features.Search.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Бэкенд workspace-поиска для префиксов палитры <c>t:</c>/<c>m:</c>/<c>x:</c> (ADR 0112).</summary>
internal interface ICommandPaletteGoToSearchBackend
{
    Task<(IReadOnlyList<RipgrepWorkspaceMatch> Matches, string? Error)> SearchMatchesAsync(
        GoToAllQuery query,
        string workspaceRoot,
        string? solutionPath,
        int maxMatches,
        string? rgExecutable,
        CancellationToken cancellationToken);
}
