#nullable enable
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Связывает wire <see cref="CodeNavigationMapGraphKind"/> и уровень карты с <see cref="CodeNavigationMapGraphPresentationKind"/> для мини-карты.</summary>
public static class CodeNavigationMapPresentationResolver
{
    public static CodeNavigationMapGraphPresentationKind Resolve(CodeNavigationMapSubgraphDocument doc, string semanticMapLevel)
    {
        if (doc.GraphKind != CodeNavigationMapGraphKind.Unspecified)
            return ToPresentation(doc.GraphKind);

        var level = CodeNavigationMapLevelKind.Normalize(semanticMapLevel);
        return string.Equals(level, CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal)
            ? CodeNavigationMapGraphPresentationKind.CodeControlFlow
            : CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles;
    }

    private static CodeNavigationMapGraphPresentationKind ToPresentation(CodeNavigationMapGraphKind kind) =>
        kind switch
        {
            CodeNavigationMapGraphKind.CodeIntent => CodeNavigationMapGraphPresentationKind.CodeControlFlow,
            CodeNavigationMapGraphKind.RelatedFiles => CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles,
            CodeNavigationMapGraphKind.RepositoryModuleTree => CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles,
            _ => CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles
        };
}
