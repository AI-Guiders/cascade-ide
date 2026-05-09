using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: git (<see cref="IdeMcpGitWorkspaceSession"/>).</summary>
public partial class MainWindowViewModel
{
    Task<string> Services.IIdeMcpActions.GitStatusAsync() =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitStatusAsync());

    Task<string> Services.IIdeMcpActions.GitDiffAsync(string? path, bool staged) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitDiffAsync(path, staged));

    Task<string> Services.IIdeMcpActions.GitLogAsync(int n) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitLogAsync(n));

    Task<string> Services.IIdeMcpActions.GitFetchAsync(string? remote, bool all, bool prune, bool dryRun) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitFetchAsync(remote, all, prune, dryRun));

    Task<string> Services.IIdeMcpActions.GitPullAsync(string? remote, string? branch, bool ffOnly, bool dryRun) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitPullAsync(remote, branch, ffOnly, dryRun));

    Task<string> Services.IIdeMcpActions.GitBranchAsync(string? action, string? name, string? startPoint, bool force) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitBranchAsync(action, name, startPoint, force));

    Task<string> Services.IIdeMcpActions.GitShowAsync(string rev, string? path, bool statOnly) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitShowAsync(rev, path, statOnly));

    Task<string> Services.IIdeMcpActions.GitSubmoduleAsync(string? action, string? path, bool recursive) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitSubmoduleAsync(action, path, recursive));

    Task<string> Services.IIdeMcpActions.GitPreflightAsync(bool staged, bool includeUntracked, bool includePatches) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitPreflightAsync(staged, includeUntracked, includePatches));

    Task<string> Services.IIdeMcpActions.GitPreflightFixSafeAsync(bool includePatches) =>
        IdeMcpGitOrchestrator.RunWithWorkspaceSession(_gitRunner, GetWorkspacePath(), s => s.GitPreflightFixSafeAsync(includePatches));

    async Task<string> Services.IIdeMcpActions.GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (!IdeMcpGitWorkspaceSession.TryCreate(_gitRunner, GetWorkspacePath(), out var session, out var err))
            return err;
        var json = await session.GitCommitAsync(message, paths).ConfigureAwait(false);
        _ = RefreshGitSummaryAsync();
        return json;
    }

    Task<string> Services.IIdeMcpActions.GitPushAsync(string? remote, string? branch, bool dryRun)
    {
        if (!IdeMcpGitWorkspaceSession.TryCreate(_gitRunner, GetWorkspacePath(), out var session, out var err))
            return Task.FromResult(err);
        if (!dryRun)
            _ = RefreshGitSummaryAsync();
        return session.GitPushAsync(remote, branch, dryRun);
    }
}
