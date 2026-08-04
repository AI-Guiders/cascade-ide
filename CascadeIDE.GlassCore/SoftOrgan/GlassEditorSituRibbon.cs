#nullable enable

namespace CascadeIDE.SoftOrgan;

/// <summary>
/// Shared-SSOT Editor situ (Q2): WHY + blast + role glance + diff intent + applies on locus.
/// ROLE = map membership only; hops = neighborhood size; LookMap = Radio pointer (not Trunc rebus).
/// </summary>
public static class GlassEditorSituRibbon
{
    /// <summary>Human/agent face pair — ECAM cards + surface.file_situ.</summary>
    public sealed record Face(
        string Why,
        string Blast,
        IReadOnlyList<string> BlastNames,
        string RoleInGraph,
        int HopNodes,
        int HopEdges,
        bool Orphan,
        string LookMap,
        string DiffIntent,
        GlassEditorDiffIntent.Face? Diff,
        string AppliesOnLocus,
        GlassEditorAppliesLocus.Face? Applies)
    {
        public bool HasAny =>
            !string.IsNullOrWhiteSpace(Why)
            || !string.IsNullOrWhiteSpace(Blast)
            || !string.IsNullOrWhiteSpace(RoleInGraph)
            || !string.IsNullOrWhiteSpace(LookMap)
            || HopNodes > 0
            || HopEdges > 0
            || !string.IsNullOrWhiteSpace(DiffIntent)
            || !string.IsNullOrWhiteSpace(AppliesOnLocus);

        public string HopLine =>
            HopNodes <= 0 && HopEdges <= 0
                ? string.Empty
                : $"{HopNodes} узлов · {HopEdges} связей";
    }

    public static Face Build(
        string? editorPath,
        string? workspaceRoot,
        string? why,
        string? leaf,
        int blastMax = 3,
        string? sourceText = null)
    {
        if (string.IsNullOrWhiteSpace(editorPath))
            return new Face("", "", [], "", 0, 0, Orphan: true, "", "", null, "", null);

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

        var graph = GlassSemanticMapGraph.Collect(workspaceRoot, editorPath, maxNodes: 48, maxHop: 2);
        var orphan = graph.Nodes.Count == 0;
        // Human labels — membership only (no Trunc rebus packing hops + look).
        var roleLine = orphan ? "сирота" : "в карте";
        const string lookMap = "карта → MFD";

        var diff = GlassEditorDiffIntent.Collect(workspaceRoot, editorPath);
        var diffLine = string.IsNullOrWhiteSpace(diff.Line) ? "" : Truncate(diff.Line, 48);

        var applies = GlassEditorAppliesLocus.Collect(editorPath, sourceText);
        var appliesLine = string.IsNullOrWhiteSpace(applies.Line) ? "" : Truncate(applies.Line, 56);

        return new Face(
            whyLine,
            blastLine,
            names,
            roleLine,
            graph.Nodes.Count,
            graph.Edges.Count,
            orphan,
            lookMap,
            diffLine,
            diff,
            appliesLine,
            applies);
    }

    public static string Format(
        string? editorPath,
        string? workspaceRoot,
        string? why,
        string? leaf,
        int blastMax = 3,
        string? sourceText = null)
    {
        var face = Build(editorPath, workspaceRoot, why, leaf, blastMax, sourceText);
        if (!face.HasAny)
            return string.Empty;

        var parts = new List<string>(7);
        if (!string.IsNullOrWhiteSpace(face.Why))
            parts.Add("WHY · " + face.Why);
        if (!string.IsNullOrWhiteSpace(face.Blast))
            parts.Add("BLAST · " + face.Blast);
        if (!string.IsNullOrWhiteSpace(face.RoleInGraph))
            parts.Add("ROLE · " + face.RoleInGraph);
        if (!string.IsNullOrWhiteSpace(face.HopLine))
            parts.Add("HOPS · " + face.HopLine);
        if (!string.IsNullOrWhiteSpace(face.LookMap))
            parts.Add("LOOK · " + face.LookMap);
        if (!string.IsNullOrWhiteSpace(face.DiffIntent))
            parts.Add("DIFF · " + face.DiffIntent);
        if (!string.IsNullOrWhiteSpace(face.AppliesOnLocus))
            parts.Add("APPLIES · " + face.AppliesOnLocus);
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
