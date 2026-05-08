using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Services;
using GitMcp.Core;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>
/// MCP-обёртка над <see cref="IGitCommandRunner"/> для текущего workspace: команды GitMcp.Core + формирование JSON ответов.
/// ViewModel задаёт только путь каталога и делегирует сюды.
/// </summary>
public sealed class IdeMcpGitWorkspaceSession
{
    private static readonly string[] PreflightSafeFixAppliedCommands = ["git add --renormalize ."];

    private readonly IGitCommandRunner _runner;
    private readonly string _workspace;

    private IdeMcpGitWorkspaceSession(IGitCommandRunner runner, string workspace)
    {
        _runner = runner;
        _workspace = workspace;
    }

    /// <summary>
    /// Невалидный каталог — JSON как у неуспешного <c>git</c>-шага (<c>exit_code</c> -1).
    /// </summary>
    public static bool TryCreate(
        IGitCommandRunner runner,
        string? workspacePath,
        [NotNullWhen(true)] out IdeMcpGitWorkspaceSession? session,
        out string errorJson)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            session = null;
            errorJson = IdeMcpGitOrchestrator.BuildCommandResult(
                false,
                -1,
                IdeMcpGitOrchestrator.WorkspaceUnavailableMessage());
            return false;
        }

        session = new IdeMcpGitWorkspaceSession(runner, workspacePath);
        errorJson = "";
        return true;
    }

    public Task<string> GitStatusAsync() =>
        RunGitCommandJsonAsync(GitCommandBuilder.StatusShortBranch());

    public Task<string> GitDiffAsync(string? path, bool staged) =>
        RunGitCommandJsonAsync(GitCommandBuilder.Diff(staged, path));

    public Task<string> GitLogAsync(int n) =>
        RunGitCommandJsonAsync(GitCommandBuilder.Log(n));

    public Task<string> GitFetchAsync(string? remote, bool all, bool prune, bool dryRun)
    {
        var r = GitCommandBuilder.Fetch(all, prune, remote, dryRun);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
    }

    public Task<string> GitPullAsync(string? remote, string? branch, bool ffOnly, bool dryRun)
    {
        var r = GitCommandBuilder.Pull(remote, branch, ffOnly, dryRun);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
    }

    public async Task<string> GitBranchAsync(string? action, string? name, string? startPoint, bool force)
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

    public Task<string> GitShowAsync(string rev, string? path, bool statOnly)
    {
        var r = GitCommandBuilder.Show(rev, path, statOnly);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(IdeMcpGitOrchestrator.BuildValidationError(r.Error!));
    }

    public Task<string> GitSubmoduleAsync(string? action, string? path, bool recursive)
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

    public async Task<string> GitPreflightAsync(bool staged, bool includeUntracked, bool includePatches)
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

    public async Task<string> GitPreflightFixSafeAsync(bool includePatches)
    {
        var renormResult = await RunGitCommandAsync(GitCommandBuilder.AddRenormalize()).ConfigureAwait(false);
        if (!renormResult.Success)
            return IdeMcpGitOrchestrator.BuildCommandResult(false, renormResult.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(renormResult.Output, 4000));

        var post = await GitPreflightAsync(staged: false, includeUntracked: true, includePatches).ConfigureAwait(false);
        return IdeMcpGitOrchestrator.BuildPreflightFixSafeResult(post, PreflightSafeFixAppliedCommands);
    }

    public async Task<string> GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (string.IsNullOrWhiteSpace(message))
            return IdeMcpGitOrchestrator.BuildMissingCommitMessageError();

        var addResult = await RunGitCommandAsync(GitCommandBuilder.Add(paths)).ConfigureAwait(false);
        if (!addResult.Success)
            return IdeMcpGitOrchestrator.BuildStepFailure("add", addResult.ExitCode, addResult.Output);

        var commitResult = await RunGitCommandAsync(GitCommandBuilder.Commit(message)).ConfigureAwait(false);
        return IdeMcpGitOrchestrator.BuildCommitResult(commitResult.Success, commitResult.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(commitResult.Output, 4000));
    }

    public Task<string> GitPushAsync(string? remote, string? branch, bool dryRun)
    {
        var args = GitCommandBuilder.Push(remote, branch, defaultOriginWhenRemoteEmpty: false, dryRun);
        return RunGitCommandJsonAsync(args);
    }

    private async Task<string> RunGitCommandJsonAsync(IReadOnlyList<string> args)
    {
        var result = await RunGitCommandAsync(args).ConfigureAwait(false);
        return IdeMcpGitOrchestrator.BuildCommandResult(result.Success, result.ExitCode, IdeMcpGitOrchestrator.TruncateOutput(result.Output, 4000));
    }

    private Task<(bool Success, int ExitCode, string Output)> RunGitCommandAsync(IReadOnlyList<string> args) =>
        _runner.RunAsync(args, _workspace);
}
