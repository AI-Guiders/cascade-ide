namespace CascadeIDE.Features.Intercom.Transport;

/// <summary>Чтение <c>origin</c> remote из git workspace (ADR 0144 §2.3.1).</summary>
public static class IntercomWorkspaceGitRemoteResolver
{
    public static string? TryGetOriginRemoteUrl(string? workspaceRoot)
    {
        var gitRoot = FindGitRoot(workspaceRoot);
        if (gitRoot is null)
            return null;

        var configPath = Path.Combine(gitRoot, ".git", "config");
        if (!File.Exists(configPath))
            return TryGitCli(gitRoot);

        try
        {
            var lines = File.ReadAllLines(configPath);
            var inOrigin = false;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith('['))
                {
                    inOrigin = trimmed.Equals("[remote \"origin\"]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inOrigin)
                    continue;

                if (trimmed.StartsWith("url =", StringComparison.OrdinalIgnoreCase))
                    return trimmed["url =".Length..].Trim();
            }
        }
        catch
        {
            return TryGitCli(gitRoot);
        }

        return TryGitCli(gitRoot);
    }

    public static string? TryGetNormalizedOrigin(string? workspaceRoot)
    {
        var raw = TryGetOriginRemoteUrl(workspaceRoot);
        return IntercomGitRepoUrlNormalizer.TryNormalize(raw);
    }

    private static string? FindGitRoot(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        var dir = new DirectoryInfo(Path.GetFullPath(workspaceRoot));
        while (dir is not null)
        {
            var git = Path.Combine(dir.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
                return dir.FullName;
            dir = dir.Parent;
        }

        return null;
    }

    private static string? TryGitCli(string gitRoot)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "git",
                Arguments = "-C \"" + gitRoot + "\" remote get-url origin",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
                return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(3000);
            return proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }
}
