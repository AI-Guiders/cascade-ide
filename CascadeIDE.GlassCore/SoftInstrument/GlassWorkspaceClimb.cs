#nullable enable

namespace CascadeIDE.SoftInstrument;

/// <summary>
/// Cold Glass may have empty session <c>WorkspaceRoot</c> — climb from exe/cwd like DomainBoard.
/// Glance pages must paint (not <c>glance · unavailable</c>) when root is null.
/// Prefer strong workspace markers (.git / .sln / workspace.toml) over thin (.cascade-ide / .cdp alone).
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

        string? best = null;
        var bestScore = 0;
        foreach (var climb in Candidates())
        {
            try
            {
                if (!Directory.Exists(climb))
                    continue;
                var score = Score(climb);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = climb;
                }
            }
            catch
            {
                // next candidate
            }
        }

        if (best is not null)
            return best;

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

    /// <summary>Higher = better workspace root. Strong markers beat thin habitat dirs.</summary>
    internal static int Score(string climb)
    {
        var score = 0;
        if (File.Exists(Path.Combine(climb, ".cascade", "workspace.toml")))
            score += 100;
        if (Directory.Exists(Path.Combine(climb, ".git")) || File.Exists(Path.Combine(climb, ".git")))
            score += 80;
        if (Directory.EnumerateFiles(climb, "*.sln").Any()
            || Directory.EnumerateFiles(climb, "*.slnx").Any())
            score += 60;
        if (Directory.Exists(Path.Combine(climb, ".cascade-ide")))
            score += 10;
        if (Directory.Exists(Path.Combine(climb, ".cdp")))
            score += 8;
        return score;
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

            for (var i = 0; i < 12 && !string.IsNullOrEmpty(cur); i++)
            {
                if (seen.Add(cur))
                    yield return cur;
                cur = Path.GetDirectoryName(cur);
            }
        }
    }
}
