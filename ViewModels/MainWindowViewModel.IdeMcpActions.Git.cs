using CascadeIDE.Features.IdeMcp.Application;
using GitMcp.Core;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: git.</summary>
public partial class MainWindowViewModel
{
    private static readonly string[] GitPreflightSafeFixAppliedCommands = ["git add --renormalize ."];

    Task<string> Services.IIdeMcpActions.GitStatusAsync() =>
        RunGitCommandJsonAsync(GitCommandBuilder.StatusShortBranch());

    Task<string> Services.IIdeMcpActions.GitDiffAsync(string? path, bool staged) =>
        RunGitCommandJsonAsync(GitCommandBuilder.Diff(staged, path));

    Task<string> Services.IIdeMcpActions.GitLogAsync(int n) =>
        RunGitCommandJsonAsync(GitCommandBuilder.Log(n));

    Task<string> Services.IIdeMcpActions.GitFetchAsync(string? remote, bool all, bool prune, bool dryRun)
    {
        var r = GitCommandBuilder.Fetch(all, prune, remote, dryRun);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
    }

    Task<string> Services.IIdeMcpActions.GitPullAsync(string? remote, string? branch, bool ffOnly, bool dryRun)
    {
        var r = GitCommandBuilder.Pull(remote, branch, ffOnly, dryRun);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
    }

    async Task<string> Services.IIdeMcpActions.GitBranchAsync(string? action, string? name, string? startPoint, bool force)
    {
        var a = IdeMcpGitOrchestrator.NormalizeAction(action, "list");
        switch (a)
        {
            case "list":
                return await RunGitCommandJsonAsync(GitCommandBuilder.BranchList().Args!).ConfigureAwait(false);
            case "create":
                var cr = GitCommandBuilder.BranchCreate(name ?? "", startPoint);
                return cr.IsSuccess
                    ? await RunGitCommandJsonAsync(cr.Args!).ConfigureAwait(false)
                    : IdeMcpGitOrchestrator.BuildValidationError(cr.Error!);
            case "delete":
                var dr = GitCommandBuilder.BranchDelete(name ?? "", force);
                return dr.IsSuccess
                    ? await RunGitCommandJsonAsync(dr.Args!).ConfigureAwait(false)
                    : IdeMcpGitOrchestrator.BuildValidationError(dr.Error!);
            default:
                return IdeMcpGitOrchestrator.BuildInvalidGitBranchActionError();
        }
    }

    Task<string> Services.IIdeMcpActions.GitShowAsync(string rev, string? path, bool statOnly)
    {
        var r = GitCommandBuilder.Show(rev, path, statOnly);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
    }

    Task<string> Services.IIdeMcpActions.GitSubmoduleAsync(string? action, string? path, bool recursive)
    {
        var a = IdeMcpGitOrchestrator.NormalizeAction(action, "status");
        switch (a)
        {
            case "status":
                return RunGitCommandJsonAsync(GitCommandBuilder.SubmoduleStatus().Args!);
            case "update":
                var r = GitCommandBuilder.SubmoduleUpdate(recursive, path);
                return r.IsSuccess
                    ? RunGitCommandJsonAsync(r.Args!)
                    : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
            default:
                return Task.FromResult(IdeMcpGitOrchestrator.BuildInvalidGitSubmoduleActionError());
        }
    }

    async Task<string> Services.IIdeMcpActions.GitPreflightAsync(bool staged, bool includeUntracked, bool includePatches)
    {
        var changedOutput = await RunGitCommandAsync(GitCommandBuilder.DiffNameOnly(staged)).ConfigureAwait(false);
        if (!changedOutput.Success)
            return IdeMcpGitOrchestrator.BuildCommandResult(false, changedOutput.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(changedOutput.Output, 4000));

        var ignoreCrOutput = await RunGitCommandAsync(GitCommandBuilder.DiffNameOnly(staged, ignoreCrAtEol: true)).ConfigureAwait(false);
        if (!ignoreCrOutput.Success)
            return IdeMcpGitOrchestrator.BuildCommandResult(false, ignoreCrOutput.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(ignoreCrOutput.Output, 4000));

        var ignoreWsOutput = await RunGitCommandAsync(GitCommandBuilder.DiffNameOnly(staged, ignoreWhitespace: true, ignoreCrAtEol: true)).ConfigureAwait(false);
        if (!ignoreWsOutput.Success)
            return IdeMcpGitOrchestrator.BuildCommandResult(false, ignoreWsOutput.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(ignoreWsOutput.Output, 4000));

        var changed = GitPreflight.ParseNameOnlyOutput(changedOutput.Output);
        var untracked = includeUntracked
            ? GitPreflight.ParseNameOnlyOutput((await RunGitCommandAsync(GitCommandBuilder.ListUntracked()).ConfigureAwait(false)).Output)
            : [];
        var ignoreCr = GitPreflight.ParseNameOnlyOutput(ignoreCrOutput.Output);
        var ignoreWs = GitPreflight.ParseNameOnlyOutput(ignoreWsOutput.Output);

        Dictionary<string, string>? patches = null;
        if (includePatches && changed.Count > 0)
        {
            patches = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in changed)
            {
                var patchArgs = GitCommandBuilder.DiffPatchForPath(staged, file);
                if (!patchArgs.IsSuccess)
                    continue;
                var patchResult = await RunGitCommandAsync(patchArgs.Args!).ConfigureAwait(false);
                if (patchResult.Success)
                    patches[file] = patchResult.Output;
            }
        }

        var report = GitPreflight.BuildReport(changed, untracked, ignoreCr, ignoreWs, patches);
        return IdeMcpGitOrchestrator.BuildPreflightReport(staged, report);
    }

    async Task<string> Services.IIdeMcpActions.GitPreflightFixSafeAsync(bool includePatches)
    {
        var renormResult = await RunGitCommandAsync(GitCommandBuilder.AddRenormalize()).ConfigureAwait(false);
        if (!renormResult.Success)
            return IdeMcpGitOrchestrator.BuildCommandResult(false, renormResult.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(renormResult.Output, 4000));

        var post = await ((Services.IIdeMcpActions)this).GitPreflightAsync(staged: false, includeUntracked: true, includePatches: includePatches).ConfigureAwait(false);
        return IdeMcpGitOrchestrator.BuildPreflightFixSafeResult(post, GitPreflightSafeFixAppliedCommands);
    }

    async Task<string> Services.IIdeMcpActions.GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (string.IsNullOrWhiteSpace(message))
            return IdeMcpGitOrchestrator.BuildMissingCommitMessageError();

        var addResult = await RunGitCommandAsync(GitCommandBuilder.Add(paths)).ConfigureAwait(false);
        if (!addResult.Success)
            return IdeMcpGitOrchestrator.BuildStepFailure("add", addResult.ExitCode, addResult.Output);

        var commitResult = await RunGitCommandAsync(GitCommandBuilder.Commit(message)).ConfigureAwait(false);
        _ = RefreshGitSummaryAsync();
        return IdeMcpGitOrchestrator.BuildCommitResult(commitResult.Success, commitResult.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(commitResult.Output, 4000));
    }

    Task<string> Services.IIdeMcpActions.GitPushAsync(string? remote, string? branch, bool dryRun)
    {
        var args = GitCommandBuilder.Push(remote, branch, defaultOriginWhenRemoteEmpty: false, dryRun);
        if (!dryRun)
            _ = RefreshGitSummaryAsync();
        return RunGitCommandJsonAsync(args);
    }

    private async Task<string> RunGitCommandJsonAsync(IReadOnlyList<string> args)
    {
        var result = await RunGitCommandAsync(args).ConfigureAwait(false);
        return IdeMcpGitOrchestrator.BuildCommandResult(result.Success, result.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(result.Output, 4000));
    }

    private async Task<(bool Success, int ExitCode, string Output)> RunGitCommandAsync(IReadOnlyList<string> args)
    {
        var workspace = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
            return (false, -1, IdeMcpGitOrchestrator.WorkspaceUnavailableMessage());
        return await _gitRunner.RunAsync(args, workspace).ConfigureAwait(false);
    }
}
