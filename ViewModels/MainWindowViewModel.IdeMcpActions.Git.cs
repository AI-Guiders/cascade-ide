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

    Task<string> Services.IIdeMcpActions.GitFetchAsync(string? remote, bool all, bool prune, bool dryRun)
    {
        var r = GitCommandBuilder.Fetch(all, prune, remote, dryRun);
        return r.IsSuccess
            ? RunGitCommandJsonAsync(r.Args!)
            : Task.FromResult(GitValidationError(r.Error!));
    }

    Task<string> Services.IIdeMcpActions.GitPullAsync(string? remote, string? branch, bool ffOnly, bool dryRun)
    {
        var r = GitCommandBuilder.Pull(remote, branch, ffOnly, dryRun);
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

    async Task<string> Services.IIdeMcpActions.GitPreflightAsync(bool staged, bool includeUntracked, bool includePatches)
    {
        var changedOutput = await RunGitCommandAsync(GitCommandBuilder.DiffNameOnly(staged)).ConfigureAwait(false);
        if (!changedOutput.Success)
            return JsonSerializer.Serialize(new { success = false, exit_code = changedOutput.ExitCode, output = TruncateOutput(changedOutput.Output, 4000) });

        var ignoreCrOutput = await RunGitCommandAsync(GitCommandBuilder.DiffNameOnly(staged, ignoreCrAtEol: true)).ConfigureAwait(false);
        if (!ignoreCrOutput.Success)
            return JsonSerializer.Serialize(new { success = false, exit_code = ignoreCrOutput.ExitCode, output = TruncateOutput(ignoreCrOutput.Output, 4000) });

        var ignoreWsOutput = await RunGitCommandAsync(GitCommandBuilder.DiffNameOnly(staged, ignoreWhitespace: true, ignoreCrAtEol: true)).ConfigureAwait(false);
        if (!ignoreWsOutput.Success)
            return JsonSerializer.Serialize(new { success = false, exit_code = ignoreWsOutput.ExitCode, output = TruncateOutput(ignoreWsOutput.Output, 4000) });

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
        return JsonSerializer.Serialize(new
        {
            success = true,
            staged,
            changed_files = report.ChangedFiles,
            untracked_files = report.UntrackedFiles,
            semantic_files = report.SemanticFiles,
            whitespace_only_files = report.WhitespaceOnlyFiles,
            eol_only_files = report.EolOnlyFiles,
            bom_only_files = report.BomOnlyFiles,
            suggested_safe_fix_commands = report.SuggestedSafeFixCommands
        });
    }

    async Task<string> Services.IIdeMcpActions.GitPreflightFixSafeAsync(bool includePatches)
    {
        var renormResult = await RunGitCommandAsync(GitCommandBuilder.AddRenormalize()).ConfigureAwait(false);
        if (!renormResult.Success)
            return JsonSerializer.Serialize(new { success = false, exit_code = renormResult.ExitCode, output = TruncateOutput(renormResult.Output, 4000) });

        var post = await ((Services.IIdeMcpActions)this).GitPreflightAsync(staged: false, includeUntracked: true, includePatches: includePatches).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(post);
        if (!doc.RootElement.TryGetProperty("success", out var ok) || ok.ValueKind != JsonValueKind.True)
            return post;

        var changed = doc.RootElement.GetProperty("changed_files");
        var untracked = doc.RootElement.GetProperty("untracked_files");
        var semantic = doc.RootElement.GetProperty("semantic_files");
        var ws = doc.RootElement.GetProperty("whitespace_only_files");
        var eol = doc.RootElement.GetProperty("eol_only_files");
        var bom = doc.RootElement.GetProperty("bom_only_files");
        var safe = doc.RootElement.GetProperty("suggested_safe_fix_commands");
        return JsonSerializer.Serialize(new
        {
            success = true,
            applied = new[] { "git add --renormalize ." },
            changed_files = changed,
            untracked_files = untracked,
            semantic_files = semantic,
            whitespace_only_files = ws,
            eol_only_files = eol,
            bom_only_files = bom,
            suggested_safe_fix_commands = safe
        });
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

    Task<string> Services.IIdeMcpActions.GitPushAsync(string? remote, string? branch, bool dryRun)
    {
        var args = GitCommandBuilder.Push(remote, branch, defaultOriginWhenRemoteEmpty: false, dryRun);
        if (!dryRun)
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
