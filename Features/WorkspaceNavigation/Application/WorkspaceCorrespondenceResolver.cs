#nullable enable

using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>ADR/feature/doc correspondence для якоря карты навигации (ADR 0061).</summary>
public static class WorkspaceCorrespondenceResolver
{
    public sealed record Result(
        string FeatureLine,
        string[] FeatureDocPaths,
        string DocsCoverageLine,
        string AdrLine,
        string? AdrFirstDocPath);

    public static Result Resolve(string? workspaceRoot, string? navigationPath)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(navigationPath))
            return Empty;

        try
        {
            var root = workspaceRoot.Trim();
            var parsed = RepositoryWorkspaceTomlLoader.TryLoad(root);
            var feature = WorkspaceFeatureResolver.ResolveFeatureFromWorkspaceToml(parsed, root, navigationPath.Trim());
            var featureLine = WorkspaceFeatureResolver.BuildFeatureLine(feature);
            var featureDocs = feature?.Docs is { Count: > 0 }
                ? feature.Docs.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0).ToArray()
                : [];

            var docPaths = new List<string>();
            if (feature?.Docs is { Count: > 0 })
                docPaths.AddRange(feature.Docs.Select(x => (x ?? "").Trim()).Where(x => x.Length > 0));

            var mapped = WorkspaceAdrMapResolver.ResolveAdrDocPathsFromWorkspaceToml(parsed, root, navigationPath.Trim());
            foreach (var m in mapped)
            {
                if (docPaths.Contains(m, StringComparer.OrdinalIgnoreCase))
                    continue;
                docPaths.Add(m);
            }

            var autoInclude = WorkspaceAdrMapResolver.NormalizeAutoInclude(parsed?.Workspace?.Adr?.AutoInclude);
            var maxRelated = parsed?.Workspace?.Adr?.MaxRelated is int mr && mr > 0 ? mr : 8;
            if (autoInclude == WorkspaceAdrMapResolver.AutoIncludeLinked && docPaths.Count > 0)
            {
                var primary = docPaths[0];
                var absPrimary = WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, primary);
                if (!string.IsNullOrWhiteSpace(absPrimary)
                    && WorkspaceTextFileReader.TryReadAllText(absPrimary, out var md))
                {
                    var linked = WorkspaceAdrMapResolver.ExtractLinkedAdrDocPathsFromMarkdown(
                        md,
                        primary,
                        parsed?.Workspace?.Adr);
                    if (linked.Count > 0)
                    {
                        var merged = new List<string>(docPaths);
                        foreach (var l in linked)
                        {
                            if (merged.Count >= docPaths.Count + maxRelated)
                                break;
                            if (merged.Contains(l, StringComparer.OrdinalIgnoreCase))
                                continue;
                            merged.Add(l);
                        }

                        docPaths = merged;
                    }
                }
            }

            var adrLine = WorkspaceAdrMapResolver.BuildAdrIndicatorLine(docPaths, parsed?.Workspace?.Adr);
            var firstRel = docPaths.Count > 0 ? docPaths[0] : null;
            var firstAbs = firstRel is null ? null : WorkspaceAdrMapResolver.TryResolveAbsoluteDocPath(root, firstRel);

            var templates = DocsTemplatesCatalogResolver.ResolveTemplatesFromWorkspaceToml(parsed, root);
            var featureTemplate = templates.FirstOrDefault(t =>
                string.Equals(t.Kind, "feature_doc", StringComparison.OrdinalIgnoreCase));
            var moduleTemplate = templates.FirstOrDefault(t =>
                string.Equals(t.Kind, "module_doc", StringComparison.OrdinalIgnoreCase));

            var featureTemplateHint = featureTemplate?.RepoPath ?? "docs/templates/feature.md";
            var moduleTemplateHint = moduleTemplate?.RepoPath ?? "docs/templates/module.md";

            var docsCoverageLine =
                docPaths.Count > 0
                    ? ""
                    : feature is not null
                        ? $"Docs: missing (feature has no ADRs) · template: {featureTemplateHint}"
                        : $"Docs: missing (no correspondence) · template: {moduleTemplateHint}";

            return new(featureLine, featureDocs, docsCoverageLine, adrLine, firstAbs);
        }
        catch
        {
            return Empty;
        }
    }

    private static Result Empty => new("", [], "", "", null);
}
