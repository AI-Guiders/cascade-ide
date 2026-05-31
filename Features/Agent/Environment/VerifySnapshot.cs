using System.Security.Cryptography;
using System.Text;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Agent.Environment;

public static class VerifySnapshot
{
    public static string Create(string solutionPath) =>
        Create(solutionPath, git: null, workspaceRoot: null);

    public static string Create(
        string solutionPath,
        IGitCommandRunner? git,
        string? workspaceRoot)
    {
        var normalized = Path.GetFullPath(solutionPath.Trim());
        var parts = new List<string> { normalized };

        if (git is not null
            && !string.IsNullOrWhiteSpace(workspaceRoot)
            && Directory.Exists(workspaceRoot))
        {
            try
            {
                parts.AddRange(CollectGitParts(git, workspaceRoot));
            }
            catch
            {
                parts.Add("tick:" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }
        else
        {
            parts.Add("tick:" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        var payload = string.Join("|", parts);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..16];
        return hash;
    }

    private static IEnumerable<string> CollectGitParts(IGitCommandRunner git, string workspaceRoot) =>
        Task.Run(() => CollectGitPartsCore(git, workspaceRoot)).GetAwaiter().GetResult();

    private static List<string> CollectGitPartsCore(IGitCommandRunner git, string workspaceRoot)
    {
        var parts = new List<string>();
        var head = TryGitHead(git, workspaceRoot);
        if (head is not null)
            parts.Add("head:" + head);

        var dirty = TryGitDirtyFingerprint(git, workspaceRoot);
        if (dirty is not null)
            parts.Add("dirty:" + dirty);

        return parts;
    }

    private static string? TryGitHead(IGitCommandRunner git, string workspaceRoot)
    {
        try
        {
            var wd = Path.GetFullPath(workspaceRoot);
            var (ok, _, output) = git.RunAsync(["-c", "core.quotepath=false", "rev-parse", "HEAD"], wd)
                .GetAwaiter().GetResult();
            if (!ok)
                return null;

            var head = output.Trim();
            return head.Length >= 7 ? head[..7] : head;
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGitDirtyFingerprint(IGitCommandRunner git, string workspaceRoot)
    {
        try
        {
            var wd = Path.GetFullPath(workspaceRoot);
            var unstaged = git.RunAsync(["-c", "core.quotepath=false", "diff", "--name-only"], wd)
                .GetAwaiter().GetResult();
            var staged = git.RunAsync(["-c", "core.quotepath=false", "diff", "--name-only", "--cached"], wd)
                .GetAwaiter().GetResult();
            if (!unstaged.Success && !staged.Success)
                return null;

            var merged = AgentDiagnoseFilesCsScopeParser.MergeGitNameOnlyOutputs(unstaged.Output, staged.Output);
            if (merged.Count == 0)
                return "clean";

            var payload = string.Join("\n", merged.OrderBy(s => s, StringComparer.OrdinalIgnoreCase));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..12];
        }
        catch
        {
            return null;
        }
    }
}
