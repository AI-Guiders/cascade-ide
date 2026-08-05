#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Cold Glass may have empty session <c>WorkspaceRoot</c> — climb from exe/cwd like DomainBoard.
/// Glance pages must paint (not <c>glance · unavailable</c>) when root is null.
/// </summary>
public static class GlassWorkspaceClimb
{
    public static string? ResolveRoot(string? workspaceRoot)
    {
        if (!string.IsNullOrWhiteSpace(workspaceRoot))
        {
            try { return Path.GetFullPath(workspaceRoot.Trim()); }
            catch { /* fall through to climb */ }
        }

        foreach (var climb in Candidates())
        {
            try
            {
                if (!Directory.Exists(climb))
                    continue;
                if (File.Exists(Path.Combine(climb, ".cascade", "workspace.toml"))
                    || Directory.Exists(Path.Combine(climb, ".git"))
                    || File.Exists(Path.Combine(climb, ".git"))
                    || Directory.Exists(Path.Combine(climb, ".cascade-ide"))
                    || Directory.Exists(Path.Combine(climb, ".cdp")))
                    return climb;
            }
            catch
            {
                // next candidate
            }
        }

        foreach (var climb in Candidates())
        {
            try
            {
                if (Directory.Exists(climb))
                    return climb;
            }
            catch
            {
                // next
            }
        }

        return null;
    }

    public static IEnumerable<string> Candidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var start in new[]
                 {
                     AppContext.BaseDirectory,
                     Environment.CurrentDirectory,
                 })
        {
            string? cur;
            try { cur = Path.GetFullPath(start); }
            catch { continue; }

            for (var i = 0; i < 8 && !string.IsNullOrEmpty(cur); i++)
            {
                if (seen.Add(cur))
                    yield return cur;
                cur = Path.GetDirectoryName(cur);
            }
        }
    }
}
