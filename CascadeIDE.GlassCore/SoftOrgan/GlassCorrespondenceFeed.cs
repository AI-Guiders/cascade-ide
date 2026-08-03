#nullable enable

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
