using CascadeIDE.ViewModels;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Application;

internal sealed partial class MainWindowIdeMcpHost
{

    public Task<string> GitStatusAsync() =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitStatusAsync());

    public Task<string> GitDiffAsync(string? path, bool staged) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitDiffAsync(path, staged));

    public Task<string> GitLogAsync(int n) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitLogAsync(n));

    public Task<string> GitFetchAsync(string? remote, bool all, bool prune, bool dryRun) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitFetchAsync(remote, all, prune, dryRun));

    public Task<string> GitPullAsync(string? remote, string? branch, bool ffOnly, bool dryRun) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitPullAsync(remote, branch, ffOnly, dryRun));

    public Task<string> GitBranchAsync(string? action, string? name, string? startPoint, bool force) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitBranchAsync(action, name, startPoint, force));

    public Task<string> GitShowAsync(string rev, string? path, bool statOnly) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitShowAsync(rev, path, statOnly));

    public Task<string> GitSubmoduleAsync(string? action, string? path, bool recursive) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitSubmoduleAsync(action, path, recursive));

    public Task<string> GitPreflightAsync(bool staged, bool includeUntracked, bool includePatches) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitPreflightAsync(staged, includeUntracked, includePatches));

    public Task<string> GitPreflightFixSafeAsync(bool includePatches) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_host.McpGitRunner, _host.McpGetWorkspacePath(), s => s.GitPreflightFixSafeAsync(includePatches));
    public async Task<string> GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (!IdeMcpGitWorkspaceSession.TryCreate(_host.McpGitRunner, _host.McpGetWorkspacePath(), out var session, out var err))
            return err;
        var json = await session.GitCommitAsync(message, paths).ConfigureAwait(false);
        _ = _host.McpRefreshGitSummaryAsync();
        return json;
    }

    public Task<string> GitPushAsync(string? remote, string? branch, bool dryRun)
    {
        if (!IdeMcpGitWorkspaceSession.TryCreate(_host.McpGitRunner, _host.McpGetWorkspacePath(), out var session, out var err))
            return Task.FromResult(err);
        if (!dryRun)
            _ = _host.McpRefreshGitSummaryAsync();
        return session.GitPushAsync(remote, branch, dryRun);
    }

}
