using System.IO;
using System.Text.Json;
using GitMcp.Core;

namespace CascadeIDE.ViewModels;

/// <summary>MCP: git.</summary>
public partial class MainWindowViewModel
{
    Task<string> Services.IIdeMcpActions.GitStatusAsync() =>
        RunGitCommandJsonAsync(GitCommandBuilder.StatusShortBranch());

    Task<string> Services.IIdeMcpActions.GitDiffAsync(string? path, bool staged) =>
        RunGitCommandJsonAsync(GitCommandBuilder.Diff(staged, path));

    Task<string> Services.IIdeMcpActions.GitLogAsync(int n) =>
        RunGitCommandJsonAsync(GitCommandBuilder.Log(n));

    Task<string> Services.IIdeMcpActions.GitFetchAsync(string? remote, bool all, bool prune)
    {
        var r = GitCommandBuilder.Fetch(all, prune, remote);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(GitValidationError(r.Error!));
    }

    Task<string> Services.IIdeMcpActions.GitPullAsync(string? remote, string? branch, bool ffOnly)
    {
        var r = GitCommandBuilder.Pull(remote, branch, ffOnly);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(GitValidationError(r.Error!));
    }

    async Task<string> Services.IIdeMcpActions.GitBranchAsync(string? action, string? name, string? startPoint, bool force)
    {
        var a = string.IsNullOrWhiteSpace(action) ? "list" : action.Trim();
        switch (a.ToLowerInvariant())
        {
            case "list":
                return await RunGitCommandJsonAsync(GitCommandBuilder.BranchList().Args!).ConfigureAwait(false);
            case "create":
                var cr = GitCommandBuilder.BranchCreate(name ?? "", startPoint);
                return cr.IsSuccess
                    ? await RunGitCommandJsonAsync(cr.Args!).ConfigureAwait(false)
                    : GitValidationError(cr.Error!);
            case "delete":
                var dr = GitCommandBuilder.BranchDelete(name ?? "", force);
                return dr.IsSuccess
                    ? await RunGitCommandJsonAsync(dr.Args!).ConfigureAwait(false)
                    : GitValidationError(dr.Error!);
            default:
                return GitValidationError("git_branch: action must be list, create, or delete.");
        }
    }

    Task<string> Services.IIdeMcpActions.GitShowAsync(string rev, string? path, bool statOnly)
    {
        var r = GitCommandBuilder.Show(rev, path, statOnly);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(GitValidationError(r.Error!));
    }

    Task<string> Services.IIdeMcpActions.GitSubmoduleAsync(string? action, string? path, bool recursive)
    {
        var a = string.IsNullOrWhiteSpace(action) ? "status" : action.Trim();
        switch (a.ToLowerInvariant())
        {
            case "status":
                return RunGitCommandJsonAsync(GitCommandBuilder.SubmoduleStatus().Args!);
            case "update":
                var r = GitCommandBuilder.SubmoduleUpdate(recursive, path);
                return r.IsSuccess
                    ? RunGitCommandJsonAsync(r.Args!)
                    : Task.FromResult(GitValidationError(r.Error!));
            default:
                return Task.FromResult(GitValidationError("git_submodule: action must be status or update."));
        }
    }

    async Task<string> Services.IIdeMcpActions.GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (string.IsNullOrWhiteSpace(message))
            return JsonSerializer.Serialize(new { success = false, error = "Commit message is required." });

        var addResult = await RunGitCommandAsync(GitCommandBuilder.Add(paths)).ConfigureAwait(false);
        if (!addResult.Success)
            return JsonSerializer.Serialize(new { success = false, step = "add", exit_code = addResult.ExitCode, output = addResult.Output });

        var commitResult = await RunGitCommandAsync(GitCommandBuilder.Commit(message)).ConfigureAwait(false);
        _ = RefreshGitSummaryAsync();
        return JsonSerializer.Serialize(new
        {
            success = commitResult.Success,
            exit_code = commitResult.ExitCode,
            output = TruncateOutput(commitResult.Output, 4000)
        });
    }

    Task<string> Services.IIdeMcpActions.GitPushAsync(string? remote, string? branch)
    {
        var args = GitCommandBuilder.Push(remote, branch, defaultOriginWhenRemoteEmpty: false);
        _ = RefreshGitSummaryAsync();
        return RunGitCommandJsonAsync(args);
    }

    private static string GitValidationError(string error) =>
        JsonSerializer.Serialize(new { success = false, error });

    private async Task<string> RunGitCommandJsonAsync(IReadOnlyList<string> args)
    {
        var result = await RunGitCommandAsync(args).ConfigureAwait(false);
        return JsonSerializer.Serialize(new
        {
            success = result.Success,
            exit_code = result.ExitCode,
            output = TruncateOutput(result.Output, 4000)
        });
    }

    private async Task<(bool Success, int ExitCode, string Output)> RunGitCommandAsync(IReadOnlyList<string> args)
    {
        var workspace = GetWorkspacePath();
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
            return (false, -1, "Workspace path is not available.");
        return await _gitRunner.RunAsync(args, workspace).ConfigureAwait(false);
    }

    private static string TruncateOutput(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        return text.Length > maxChars ? text[..maxChars] + "\n... (output truncated)" : text;
    }
}
