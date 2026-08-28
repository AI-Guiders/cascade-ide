#nullable enable
using AIGuiders.Platform.CommandPlane;
using CascadeIDE.Services;
using CideSemanticFields = CascadeIDE.Services.SlashSemanticFields;
using CidePathRole = CascadeIDE.Services.SlashPathRole;
using PlatformPathRole = AIGuiders.Platform.CommandPlane.SlashPathRole;
using PlatformSlashLineResolver = AIGuiders.Platform.CommandPlane.SlashLineResolver;

namespace CascadeIDE.Features.Chat;

/// <summary>Thin CIDE adapter over platform <see cref="SlashStepCompletion"/>.</summary>
internal static class SlashSemanticCatalogIndex
{
    internal enum CompletionStep
    {
        Domain,
        Object,
        Intent,
        Arg,
    }

    internal readonly record struct CompletionState(
        CompletionStep Step,
        string? Domain,
        string? Object,
        string PartialToken);

    internal static IReadOnlyList<ChatSlashSuggestion> GetSegmentSuggestions(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        string typedBody)
    {
        var catalog = SlashPlatformCatalog.Instance;
        if (PlatformSlashLineResolver.TryResolveBody(typedBody, catalog, out var line)
            && line.ShouldHideSegmentSuggestions)
            return [];

        return SlashStepCompletion
            .GetSuggestions(catalog, tokens, endsWithSpace, typedBody)
            .Select(Map)
            .ToList();
    }

    internal static bool TryResolveHierarchy(
        IReadOnlyList<string> tokens,
        bool endsWithSpace,
        out CideSemanticFields fields,
        out string matchedPath)
    {
        if (SlashStepCompletion.TryResolveHierarchy(
                SlashPlatformCatalog.Instance,
                tokens,
                endsWithSpace,
                out var platformFields,
                out matchedPath))
        {
            fields = new CideSemanticFields(
                platformFields.Domain,
                platformFields.Object,
                platformFields.Intent,
                platformFields.PathRole == PlatformPathRole.Alias ? CidePathRole.Alias : CidePathRole.Canonical);
            return true;
        }

        fields = default;
        matchedPath = "";
        return false;
    }

    static ChatSlashSuggestion Map(SlashCompletionItem item) =>
        new(item.InsertText, item.SlashPath, item.Help, item.Group, item.StepSegment);
}
