using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Сбор <c>.cs</c> из git diff (shared L0/L3).</summary>
internal static class AgentGitDirtyCsPaths
{
    public static async Task<IReadOnlyList<string>> CollectAsync(
        IGitCommandRunner git,
        string workspaceRoot,
        int maxFiles,
        CancellationToken cancellationToken = default)
    {
        var wsFull = Path.GetFullPath(workspaceRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var unstaged = await GitNameOnlyAsync(git, wsFull, ["diff", "--name-only"], cancellationToken).ConfigureAwait(false);
        var staged = await GitNameOnlyAsync(git, wsFull, ["diff", "--name-only", "--cached"], cancellationToken).ConfigureAwait(false);
        var relCs = AgentL0CsScopeParser.MergeGitNameOnlyOutputs(unstaged, staged);

        var cap = Math.Max(1, maxFiles);
        var result = new List<string>();
        foreach (var rel in relCs)
        {
            if (result.Count >= cap)
                break;

            if (!AgentL0CsScopeParser.TryResolveWorkspaceCs(wsFull, rel, out var full))
                continue;

            result.Add(full);
        }

        return result;
    }

    private static async Task<string?> GitNameOnlyAsync(
        IGitCommandRunner git,
        string workingDirectory,
        IReadOnlyList<string> gitArgsTail,
        CancellationToken cancellationToken)
    {
        var args = new List<string> { "-c", "core.quotepath=false" };
        args.AddRange(gitArgsTail);
        var (ok, _, output) = await git.RunAsync(args, workingDirectory, cancellationToken).ConfigureAwait(false);
        return ok ? output : null;
    }
}
