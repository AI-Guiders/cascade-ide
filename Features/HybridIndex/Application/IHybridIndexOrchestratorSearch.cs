using HybridCodebaseIndex.Core;

namespace CascadeIDE.Features.HybridIndex.Application;

/// <summary>
/// Узкий контракт оркестратора для палитры: статус индекса и FTS-поиск (ADR 0112).
/// Выделен для подмен в тестах без полного <see cref="HybridIndexOrchestrator"/>.
/// </summary>
public interface IHybridIndexOrchestratorSearch
{
    Task<IndexStatus> GetIndexStatusAsync(
        string workspaceRoot,
        string? solutionPath,
        CancellationToken cancellationToken = default);

    Task<(SearchResponse Response, string? Error)> SearchHybridAsync(
        string workspaceRoot,
        string? solutionPath,
        string query,
        int topN,
        string? pathPrefix,
        IReadOnlyList<string>? excludePathPrefixes,
        IReadOnlyList<string>? extensions,
        bool semantic,
        double alpha,
        double beta,
        int vecTopK,
        CancellationToken cancellationToken = default);
}
