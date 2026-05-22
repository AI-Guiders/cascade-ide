using CascadeIDE.ViewModels;
using CascadeIDE.Features.HybridIndex.McpParity;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    private bool TryResolveHybridIndexScopeForCodebaseIndexCalls(
        string? argWorkspacePath,
        string? argSolutionPath,
        out string hciWorkspaceRoot,
        out string? hciSolutionPath,
        out string? errorJson) =>
        IdeMcpHybridIndexScope.TryResolveForCodebaseIndexCommand(
            argWorkspacePath,
            argSolutionPath,
            _host.McpSettings.HybridIndex.ScopeMode,
            _host.Workspace.SolutionPath,
            WorkspaceDirectoryFromSolutionPath.Resolve,
            out hciWorkspaceRoot,
            out hciSolutionPath,
            out errorJson);

    public Task<string> CodebaseIndexStatusAsync(string? workspacePath, string? solutionPath, CancellationToken cancellationToken)
    {
        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            var st = await _host.McpHybridIndex.GetIndexStatusAsync(ws, sln, cancellationToken).ConfigureAwait(false);
            return CodebaseIndexIdeJsonResponses.SerializeStatus(st);
        }, cancellationToken);
    }

    public Task<string> CodebaseIndexSearchAsync(
        string? workspacePath,
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Task.FromResult(IdeMcpHybridCodebaseIndexOrchestrator.MissingQueryJson());

        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            var (response, searchErr) = await _host.McpHybridIndex.SearchHybridAsync(
                    ws,
                    sln,
                    query.Trim(),
                    topN,
                    pathPrefix,
                    excludePathPrefixes,
                    extensions,
                    semantic,
                    alpha,
                    beta,
                    vecTopK,
                    cancellationToken)
                .ConfigureAwait(false);
            return CodebaseIndexIdeJsonResponses.SerializeSearch(response, searchErr);
        }, cancellationToken);
    }

    public Task<string> CodebaseIndexExplainAsync(string? workspacePath, string? solutionPath, long hitId, CancellationToken cancellationToken)
    {
        if (hitId <= 0)
            return Task.FromResult(IdeMcpHybridCodebaseIndexOrchestrator.InvalidHitIdJson());

        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            var resp = await _host.McpHybridIndex.ExplainHitAsync(ws, sln, hitId, cancellationToken).ConfigureAwait(false);
            return CodebaseIndexIdeJsonResponses.SerializeExplain(resp);
        }, cancellationToken);
    }

    public Task<string> CodebaseIndexReindexAsync(string? workspacePath, string? solutionPath, bool fullRebuild, CancellationToken cancellationToken)
    {
        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            try
            {
                var summary = await _host.McpHybridIndex.RunReindexWithPublishAsync(ws, sln, fullRebuild, cancellationToken).ConfigureAwait(false);
                return CodebaseIndexIdeJsonResponses.SerializeReindex(summary);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return IdeMcpHybridCodebaseIndexOrchestrator.SerializeReindexFailed(ex.Message);
            }
        }, cancellationToken);
    }

}
