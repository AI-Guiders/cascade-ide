#nullable enable
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Связывает wire <see cref="SemanticMapGraphKind"/> и уровень карты с <see cref="SemanticMapGraphPresentationKind"/> для мини-карты.</summary>
public static class SemanticMapPresentationResolver
{
    public static SemanticMapGraphPresentationKind Resolve(SemanticMapSubgraphDocument doc, string semanticMapLevel)
    {
        if (doc.GraphKind != SemanticMapGraphKind.Unspecified)
            return ToPresentation(doc.GraphKind);

        var level = SemanticMapLevelKind.Normalize(semanticMapLevel);
        return string.Equals(level, SemanticMapLevelKind.ControlFlow, StringComparison.Ordinal)
            ? SemanticMapGraphPresentationKind.CodeControlFlow
            : SemanticMapGraphPresentationKind.WorkspaceRelatedFiles;
    }

    private static SemanticMapGraphPresentationKind ToPresentation(SemanticMapGraphKind kind) =>
        kind switch
        {
            SemanticMapGraphKind.CodeIntentSemanticMap => SemanticMapGraphPresentationKind.CodeControlFlow,
            SemanticMapGraphKind.RelatedFiles => SemanticMapGraphPresentationKind.WorkspaceRelatedFiles,
            SemanticMapGraphKind.RepositoryModuleTree => SemanticMapGraphPresentationKind.WorkspaceRelatedFiles,
            _ => SemanticMapGraphPresentationKind.WorkspaceRelatedFiles
        };
}
