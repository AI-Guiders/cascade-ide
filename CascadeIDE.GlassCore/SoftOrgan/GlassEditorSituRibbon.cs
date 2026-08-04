#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Shared-SSOT Editor situ (Q2): WHY-this-file + blast as instrument faces (not one text ribbon).
/// </summary>
public static class GlassEditorSituRibbon
{
    /// <summary>Human/agent face pair — ECAM cards + surface.file_situ.</summary>
    public sealed record Face(
        string Why,
        string Blast,
        IReadOnlyList<string> BlastNames)
    {
        public bool HasAny =>
            !string.IsNullOrWhiteSpace(Why) || !string.IsNullOrWhiteSpace(Blast);
    }

    public static Face Build(
        string? editorPath,
        string? workspaceRoot,
        string? why,
        string? leaf,
        int blastMax = 3)
    {
        if (string.IsNullOrWhiteSpace(editorPath))
            return new Face("", "", []);

        var whyBits = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(leaf))
            whyBits.Add(Truncate(leaf!.Trim(), 48));
        if (!string.IsNullOrWhiteSpace(why))
            whyBits.Add(Truncate(why!.Trim(), 72));
        var whyLine = whyBits.Count > 0 ? string.Join(" · ", whyBits) : "";

        var blast = GlassRelatedFilesFeed.Collect(workspaceRoot, editorPath, max: Math.Max(1, blastMax));
        var names = blast
            .Take(blastMax)
            .Select(i => ShortName(i.RelativePath))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        var blastLine = names.Count > 0 ? string.Join(" · ", names) : "";

        return new Face(whyLine, blastLine, names);
    }

    /// <summary>Compat dump (tests / legacy). Prefer <see cref="Build"/> for instruments.</summary>
    public static string Format(
        string? editorPath,
        string? workspaceRoot,
        string? why,
        string? leaf,
        int blastMax = 3)
    {
        var face = Build(editorPath, workspaceRoot, why, leaf, blastMax);
        if (!face.HasAny)
            return string.Empty;

        var parts = new List<string>(2);
        if (!string.IsNullOrWhiteSpace(face.Why))
            parts.Add("WHY · " + face.Why);
        if (!string.IsNullOrWhiteSpace(face.Blast))
            parts.Add("BLAST · " + face.Blast);
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
