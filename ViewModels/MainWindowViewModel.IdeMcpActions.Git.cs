using System.IO;
using System.Text.Json;

namespace CascadeIDE.ViewModels;

public partial class MainWindowViewModel
{
    Task<string> Services.IIdeMcpActions.GitStatusAsync() => RunGitCommandJsonAsync(["status", "--short", "--branch"]);

    Task<string> Services.IIdeMcpActions.GitDiffAsync(string? path, bool staged)
    {
        var args = new List<string> { "diff" };
        if (staged)
            args.Add("--staged");
        if (!string.IsNullOrWhiteSpace(path))
        {
            args.Add("--");
            args.Add(path!);
        }
        return RunGitCommandJsonAsync(args);
    }

    async Task<string> Services.IIdeMcpActions.GitCommitAsync(string message, IReadOnlyList<string>? paths)
    {
        if (string.IsNullOrWhiteSpace(message))
            return JsonSerializer.Serialize(new { success = false, error = "Commit message is required." });

        var addArgs = new List<string> { "add" };
        if (paths is { Count: > 0 })
            addArgs.AddRange(paths.Where(p => !string.IsNullOrWhiteSpace(p)));
        else
            addArgs.Add("-A");

        var addResult = await RunGitCommandAsync(addArgs).ConfigureAwait(false);
        if (!addResult.Success)
            return JsonSerializer.Serialize(new { success = false, step = "add", exit_code = addResult.ExitCode, output = addResult.Output });

        var commitResult = await RunGitCommandAsync(["commit", "-m", message]).ConfigureAwait(false);
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
        var args = new List<string> { "push" };
        if (!string.IsNullOrWhiteSpace(remote))
            args.Add(remote!);
        if (!string.IsNullOrWhiteSpace(branch))
            args.Add(branch!);
        _ = RefreshGitSummaryAsync();
        return RunGitCommandJsonAsync(args);
    }

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
