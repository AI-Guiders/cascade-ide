#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Services;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>JSON для MCP <c>get_correspondence_context</c> (ADR 0156 §3).</summary>
public static class WorkspaceCorrespondenceContextBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string BuildJson(string? workspaceRoot, string? absoluteFilePath, bool hasCodeGraph = false)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot) || string.IsNullOrWhiteSpace(absoluteFilePath))
            return JsonSerializer.Serialize(
                new CorrespondenceContextPayload(null, [], "", "", null, [], []),
                JsonOptions);

        var root = workspaceRoot.Trim();
        var file = absoluteFilePath.Trim();
        var workspaceToml = RepositoryWorkspaceTomlLoader.TryLoad(root);
        var correspondence = WorkspaceCorrespondenceResolver.Resolve(root, file);
        var explicitAnchors = WorkspaceCorrespondenceCodeAnchorsLoader.LoadFromWorkspaceToml(workspaceToml, root);
        var reverse = DocReverseAnchorResolver.Resolve(
            root,
            file,
            correspondence.AdrDocPaths,
            explicitAnchors);

        var rel = WorkspaceAdrMapResolver.TryComputeRepoRelativePath(root, file);
        var signals = CorrespondenceLayersProjection.FromCorrespondence(
            hasHciOrientation: false,
            hasFeature: !string.IsNullOrWhiteSpace(correspondence.FeatureLine),
            hasAdrDocs: correspondence.AdrDocPaths.Length > 0,
            hasCodeGraph: hasCodeGraph);

        var payload = new CorrespondenceContextPayload(
            File: rel,
            ActiveLayers: ToLayerIds(signals),
            LayersBadge: CorrespondenceLayersProjection.BuildLayersBadge(signals),
            LayersTooltip: CorrespondenceLayersProjection.BuildLayersTooltip(signals),
            Feature: string.IsNullOrWhiteSpace(correspondence.FeatureLine)
                ? null
                : new FeaturePayload(correspondence.FeatureLine, correspondence.FeatureDocPaths),
            ForwardDocs: correspondence.AdrDocPaths
                .Select(p => new ForwardDocPayload(p, WorkspaceAdrMapResolver.GuessAdrPreviewTitle(p)))
                .ToArray(),
            ReverseAnchors: reverse
                .Select(m => new ReverseAnchorPayload(
                    m.DocPath,
                    m.DocTitle,
                    m.Provenance,
                    m.Kind,
                    new CodeAnchorPayload(
                        m.CodeAnchor.File,
                        m.CodeAnchor.LineStart,
                        m.CodeAnchor.LineEnd,
                        m.CodeAnchor.MemberKey,
                        m.CodeAnchor.SyntaxScope),
                    m.Excerpt,
                    m.DocLineHint))
                .ToArray());

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static string[] ToLayerIds(CorrespondenceLayerSignals signals)
    {
        var ids = new List<string>(6);
        if (signals.SemanticCanonL0) ids.Add("L0");
        if (signals.FeatureRegistryL1p) ids.Add("L1p");
        if (signals.AdrPathMapL1) ids.Add("L1");
        if (signals.CodeIntentGraphL2) ids.Add("L2");
        if (signals.ChangeIntentL3) ids.Add("L3");
        if (signals.DiscourseCodeL4) ids.Add("L4");
        return ids.ToArray();
    }

    private sealed record CorrespondenceContextPayload(
        string? File,
        string[] ActiveLayers,
        string LayersBadge,
        string LayersTooltip,
        FeaturePayload? Feature,
        ForwardDocPayload[] ForwardDocs,
        ReverseAnchorPayload[] ReverseAnchors);

    private sealed record FeaturePayload(string Line, string[] Docs);

    private sealed record ForwardDocPayload(string Path, string Title);

    private sealed record ReverseAnchorPayload(
        string DocPath,
        string DocTitle,
        string Provenance,
        string Kind,
        CodeAnchorPayload CodeAnchor,
        string Excerpt,
        int? DocLineHint);

    private sealed record CodeAnchorPayload(
        string File,
        int? LineStart,
        int? LineEnd,
        string? MemberKey,
        string? SyntaxScope);
}
