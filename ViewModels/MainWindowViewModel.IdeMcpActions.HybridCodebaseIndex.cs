using System.Text.Json;
using CascadeIDE.Features.HybridIndex.McpParity;
using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>MCP / ide_execute_command: Hybrid Codebase Index (имена команд как у внешнего MCP).</summary>
public partial class MainWindowViewModel
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
            _settings.HybridIndex.ScopeMode,
            Workspace.SolutionPath,
            GetWorkspacePath,
            out hciWorkspaceRoot,
            out hciSolutionPath,
            out errorJson);

    Task<string> Services.IIdeMcpActions.CodebaseIndexStatusAsync(string? workspacePath, string? solutionPath, CancellationToken cancellationToken)
    {
        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            var st = await _hybridIndex.GetIndexStatusAsync(ws, sln, cancellationToken).ConfigureAwait(false);
            return CodebaseIndexIdeJsonResponses.SerializeStatus(st);
        }, cancellationToken);
    }

    Task<string> Services.IIdeMcpActions.CodebaseIndexSearchAsync(
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
            return Task.FromResult("""{"error":"missing_query"}""");

        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            var (response, searchErr) = await _hybridIndex.SearchHybridAsync(
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

    Task<string> Services.IIdeMcpActions.CodebaseIndexExplainAsync(string? workspacePath, string? solutionPath, long hitId, CancellationToken cancellationToken)
    {
        if (hitId <= 0)
            return Task.FromResult("""{"error":"invalid_hit_id"}""");

        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            var resp = await _hybridIndex.ExplainHitAsync(ws, sln, hitId, cancellationToken).ConfigureAwait(false);
            return CodebaseIndexIdeJsonResponses.SerializeExplain(resp);
        }, cancellationToken);
    }

    Task<string> Services.IIdeMcpActions.CodebaseIndexReindexAsync(string? workspacePath, string? solutionPath, bool fullRebuild, CancellationToken cancellationToken)
    {
        if (!TryResolveHybridIndexScopeForCodebaseIndexCalls(workspacePath, solutionPath, out var ws, out var sln, out var errJson))
            return Task.FromResult(errJson!);

        return Task.Run(async () =>
        {
            try
            {
                var summary = await _hybridIndex.RunReindexWithPublishAsync(ws, sln, fullRebuild, cancellationToken).ConfigureAwait(false);
                return CodebaseIndexIdeJsonResponses.SerializeReindex(summary);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = "reindex_failed", detail = ex.Message });
            }
        }, cancellationToken);
    }
}
