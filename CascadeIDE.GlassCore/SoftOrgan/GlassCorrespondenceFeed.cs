#nullable enable

using System.Globalization;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services;

namespace CascadeIDE.SoftOrgan;

/// <summary>Glass CRS list feed — shared Avalonia-free WorkspaceCorrespondenceResolver + DocReverseAnchorResolver.</summary>
public static class GlassCorrespondenceFeed
{
    public sealed record Item(string FilePath, string Kind, int? LineHint = null, string? Title = null)
    {
        public string Display
        {
            get
            {
                var name = Title is { Length: > 0 } ? Title : Path.GetFileName(FilePath);
                var line = LineHint is > 0 ? $":{LineHint}" : "";
                return $"{Kind} · {name}{line}";
            }
        }
    }

    public sealed record Snapshot(
        IReadOnlyList<Item> Reverse,
        IReadOnlyList<Item> Forward,
        string StatusLine,
        string FeatureLine,
        string AdrLine,
        string DocsCoverageLine,
        string LayersBadge);

    /// <summary>One rail row on the CRS thread timeline (human face).</summary>
    public sealed record TimelineRow(string Role, Item Item)
    {
        public string Display => Role switch
        {
            "focus" => $"◆ focus · {(Item.Title is { Length: > 0 } ? Item.Title : Path.GetFileName(Item.FilePath))}",
            "reverse" => $"◀ {Item.Display}",
            "forward" => $"▶ {Item.Display}",
            _ => Item.Display,
        };
    }

    public static IReadOnlyList<GlassGlanceChip> BuildInstrument(Snapshot snap, string? editorPath)
    {
        var focus = string.IsNullOrWhiteSpace(editorPath) ? "—" : Path.GetFileName(editorPath);
        var feature = string.IsNullOrWhiteSpace(snap.FeatureLine) || snap.FeatureLine.Contains("no feature", StringComparison.OrdinalIgnoreCase)
            ? "—"
            : Trunc(snap.FeatureLine, 28);
        var adr = string.IsNullOrWhiteSpace(snap.AdrLine) ? "—" : Trunc(snap.AdrLine, 28);
        var hasThread = snap.Reverse.Count > 0 || snap.Forward.Count > 0;
        return
        [
            new("CRS", hasThread ? "LIVE" : "IDLE", hasThread ? "ok" : "idle"),
            new("FOCUS", Trunc(focus, 28), string.IsNullOrWhiteSpace(editorPath) ? "idle" : "ok"),
            new("FEATURE", feature, feature == "—" ? "idle" : "ok"),
            new("ADR", adr, adr == "—" ? "idle" : "warn"),
            new("REV", snap.Reverse.Count.ToString(CultureInfo.InvariantCulture), snap.Reverse.Count > 0 ? "ok" : "idle"),
            new("FWD", snap.Forward.Count.ToString(CultureInfo.InvariantCulture), snap.Forward.Count > 0 ? "ok" : "idle"),
        ];
    }

    /// <summary>Thread rail: reverse (docs→code) → focus → forward (code→docs).</summary>
    public static IReadOnlyList<TimelineRow> BuildTimeline(Snapshot snap, string? editorPath, int max = 48)
    {
        var rows = new List<TimelineRow>();
        foreach (var r in snap.Reverse.Take(Math.Max(0, max / 2)))
            rows.Add(new TimelineRow("reverse", r));

        if (!string.IsNullOrWhiteSpace(editorPath) && File.Exists(editorPath))
        {
            rows.Add(new TimelineRow(
                "focus",
                new Item(editorPath, "focus", Title: Path.GetFileName(editorPath))));
        }

        foreach (var f in snap.Forward.Take(Math.Max(0, max - rows.Count)))
            rows.Add(new TimelineRow("forward", f));

        return rows;
    }

    static string Trunc(string s, int max) =>
        s.Length <= max ? s : s[..(max - 1)] + "…";

    public static Snapshot Collect(
        string? workspaceRoot,
        string? editorPath,
        int maxEach = 32)
    {
        var reverse = new List<Item>();
        var forward = new List<Item>();
        var seenF = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(workspaceRoot) || !Directory.Exists(workspaceRoot))
        {
            return new Snapshot(reverse, forward, "crs · no workspace", "", "", "", "");
        }

        var root = workspaceRoot.Trim();
        var resolved = WorkspaceCorrespondenceResolver.Resolve(root, editorPath);
        foreach (var rel in resolved.AdrDocPaths)
        {
            if (forward.Count >= maxEach)
                break;
            var abs = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, rel) ?? rel;
            if (!seenF.Add(abs))
                continue;
            forward.Add(new Item(
                abs,
                "adr",
                Title: WorkspaceAdrMapResolver.GuessAdrPreviewTitle(rel)));
        }

        foreach (var rel in resolved.FeatureDocPaths)
        {
            if (forward.Count >= maxEach)
                break;
            var abs = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, rel) ?? rel;
            if (!seenF.Add(abs))
                continue;
            forward.Add(new Item(
                abs,
                "feature",
                Title: Path.GetFileName(rel)));
        }

        if (!string.IsNullOrWhiteSpace(editorPath) && File.Exists(editorPath))
        {
            var workspaceToml = RepositoryWorkspaceTomlLoader.TryLoad(root);
            var explicitAnchors = WorkspaceCorrespondenceCodeAnchorsLoader.LoadFromWorkspaceToml(workspaceToml, root);
            var matches = DocReverseAnchorResolver.Resolve(root, editorPath, resolved.AdrDocPaths, explicitAnchors);
            foreach (var m in matches.Take(maxEach))
            {
                var abs = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, m.DocPath) ?? m.DocPath;
                reverse.Add(new Item(
                    abs,
                    m.Provenance,
                    m.DocLineHint,
                    m.DocTitle));
            }
        }

        var hasFeature = !string.IsNullOrWhiteSpace(resolved.FeatureLine)
                         && !resolved.FeatureLine.Contains("no feature", StringComparison.OrdinalIgnoreCase);
        var hasAdr = resolved.AdrDocPaths.Length > 0;
        var layers = CorrespondenceLayersProjection.FromCorrespondence(
            hasHciOrientation: false,
            hasFeature: hasFeature,
            hasAdrDocs: hasAdr,
            hasCodeGraph: false);
        var badge = CorrespondenceLayersProjection.BuildLayersBadge(layers);

        var status = $"crs · reverse {reverse.Count} · forward {forward.Count}"
                     + (badge.Length > 0 ? $" · {badge}" : "");
        if (!string.IsNullOrWhiteSpace(resolved.DocsCoverageLine))
            status += $" · {resolved.DocsCoverageLine}";

        return new Snapshot(
            reverse,
            forward,
            status,
            resolved.FeatureLine,
            resolved.AdrLine,
            resolved.DocsCoverageLine,
            badge);
    }
}
