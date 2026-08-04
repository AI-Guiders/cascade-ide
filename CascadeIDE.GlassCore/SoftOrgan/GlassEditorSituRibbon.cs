#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Shared-SSOT Editor situ (Q2): WHY-this-file + blast ribbon from plan why + RelatedFiles feed.
/// Human face under path label — not Intercom dump.
/// </summary>
public static class GlassEditorSituRibbon
{
    public static string Format(
        string? editorPath,
        string? workspaceRoot,
        string? why,
        string? leaf,
        int blastMax = 3)
    {
        if (string.IsNullOrWhiteSpace(editorPath))
            return string.Empty;

        var parts = new List<string>(2);
        var whyBits = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(leaf))
            whyBits.Add(Truncate(leaf!.Trim(), 48));
        if (!string.IsNullOrWhiteSpace(why))
            whyBits.Add(Truncate(why!.Trim(), 72));
        if (whyBits.Count > 0)
            parts.Add("WHY · " + string.Join(" · ", whyBits));

        var blast = GlassRelatedFilesFeed.Collect(workspaceRoot, editorPath, max: Math.Max(1, blastMax));
        if (blast.Count > 0)
        {
            var names = blast
                .Take(blastMax)
                .Select(i => ShortName(i.RelativePath))
                .Where(s => !string.IsNullOrWhiteSpace(s));
            var joined = string.Join(" · ", names);
            if (!string.IsNullOrWhiteSpace(joined))
                parts.Add("BLAST · " + joined);
        }

        return string.Join("  |  ", parts);
    }

    static string ShortName(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;
        var name = Path.GetFileName(relativePath.Replace('\\', '/'));
        return Truncate(name, 28);
    }

    static string Truncate(string s, int max)
    {
        s = s.Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (s.Length <= max)
            return s;
        return s[..(max - 1)].TrimEnd() + "…";
    }
}
