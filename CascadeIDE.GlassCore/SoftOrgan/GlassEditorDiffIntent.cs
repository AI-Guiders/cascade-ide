#nullable enable

using System.Text.RegularExpressions;

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// File-situ Diff intent (Q2): human-face summary + new-side line map for Editor hunk tint.
/// Not raw unified-diff dump as primary.
/// </summary>
public static class GlassEditorDiffIntent
{
    static readonly Regex HunkHeader = new(
        @"^@@\s+-(\d+)(?:,(\d+))?\s+\+(\d+)(?:,(\d+))?\s+@@",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public sealed record Face(
        string Line,
        int Added,
        int Deleted,
        int Hunks,
        IReadOnlyList<int> AddLines,
        IReadOnlyList<int> DeleteAnchors,
        bool Clean,
        bool Untracked)
    {
        public bool HasTint => AddLines.Count > 0 || DeleteAnchors.Count > 0;
    }

    public static Face Collect(string? workspaceRoot, string? editorPath)
    {
        if (string.IsNullOrWhiteSpace(editorPath) || !File.Exists(editorPath))
            return new Face("", 0, 0, 0, [], [], Clean: true, Untracked: false);

        var root = ResolveRoot(workspaceRoot, editorPath);
        if (root is null)
            return new Face("NO-GIT", 0, 0, 0, [], [], Clean: true, Untracked: false);

        string rel;
        try
        {
            rel = Path.GetRelativePath(root, Path.GetFullPath(editorPath)).Replace('\\', '/');
        }
        catch
        {
            return new Face("NO-GIT", 0, 0, 0, [], [], Clean: true, Untracked: false);
        }

        if (IsUntracked(root, rel))
            return new Face("UNTRACKED", 0, 0, 0, [], [], Clean: false, Untracked: true);

        var diff = GlassGitProcess.Run(root, "diff", "HEAD", "--", rel);
        if (!diff.Ok && string.IsNullOrWhiteSpace(diff.Output))
            return new Face("DIFF-ERR", 0, 0, 0, [], [], Clean: true, Untracked: false);

        if (string.IsNullOrWhiteSpace(diff.Output))
            return new Face("CLEAN", 0, 0, 0, [], [], Clean: true, Untracked: false);

        return Parse(diff.Output);
    }

    public static Face Parse(string unifiedDiff)
    {
        var adds = new List<int>();
        var delAnchors = new List<int>();
        var added = 0;
        var deleted = 0;
        var hunks = 0;
        var newLine = 0;

        foreach (var raw in unifiedDiff.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var m = HunkHeader.Match(raw);
            if (m.Success)
            {
                hunks++;
                newLine = int.Parse(m.Groups[3].Value);
                continue;
            }

            if (hunks == 0)
                continue;

            if (raw.StartsWith('+') && !raw.StartsWith("+++"))
            {
                adds.Add(newLine);
                added++;
                newLine++;
            }
            else if (raw.StartsWith('-') && !raw.StartsWith("---"))
            {
                deleted++;
                if (!delAnchors.Contains(newLine))
                    delAnchors.Add(newLine);
            }
            else if (raw.StartsWith(' ') || raw.Length == 0)
            {
                newLine++;
            }
        }

        var line = $"+{added} −{deleted} · {hunks}h";
        return new Face(line, added, deleted, hunks, adds, delAnchors, Clean: added == 0 && deleted == 0, Untracked: false);
    }

    static string? ResolveRoot(string? workspaceRoot, string editorPath)
    {
        try
        {
            // Prefer the git root that owns the open file (session WorkspaceRoot may be CDP seat).
            var dir = Path.GetDirectoryName(Path.GetFullPath(editorPath));
            while (!string.IsNullOrWhiteSpace(dir))
            {
                if (Directory.Exists(Path.Combine(dir, ".git")) || File.Exists(Path.Combine(dir, ".git")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }

            if (!string.IsNullOrWhiteSpace(workspaceRoot) && Directory.Exists(workspaceRoot))
            {
                var probe = GlassGitProcess.Run(workspaceRoot, "rev-parse", "--show-toplevel");
                if (probe.Ok && !string.IsNullOrWhiteSpace(probe.Output))
                    return Path.GetFullPath(probe.Output.Trim().Split('\n')[0].Trim());
            }
        }
        catch
        {
            /* ignore */
        }

        return null;
    }

    static bool IsUntracked(string root, string rel)
    {
        var st = GlassGitProcess.Run(root, "status", "--porcelain=v1", "--", rel);
        if (!st.Ok || string.IsNullOrWhiteSpace(st.Output))
            return false;
        foreach (var line in st.Output.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.Length <= 3)
                continue;
            if (line.StartsWith("??", StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
