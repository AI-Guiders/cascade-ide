using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Environment;

/// <summary>Git worktree profile (ADR 0148 W6). Best-effort: requires clean repo.</summary>
public sealed class AgentWorktreeSandbox
{
    private readonly IGitCommandRunner _git;

    public AgentWorktreeSandbox(IGitCommandRunner git) => _git = git;

    public async Task<(bool Success, string? WorktreePath, string? Error)> TryCreateAsync(
        string workspaceRoot,
        string runId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
            return (false, null, "Workspace root is missing.");

        var parent = Path.Combine(workspaceRoot, ".cascade-agent-worktrees");
        Directory.CreateDirectory(parent);
        var target = Path.Combine(parent, runId);

        if (Directory.Exists(target))
        {
            try
            {
                Directory.Delete(target, recursive: true);
            }
            catch (IOException ex)
            {
                return (false, null, ex.Message);
            }
        }

        var args = new[] { "worktree", "add", target, "-b", $"agent/{runId[..8]}" };
        var (ok, _, output) = await _git.RunAsync(args, workspaceRoot, cancellationToken).ConfigureAwait(false);
        if (!ok)
            return (false, null, string.IsNullOrWhiteSpace(output) ? "git worktree add failed." : output.Trim());

        return (true, target, null);
    }
}
