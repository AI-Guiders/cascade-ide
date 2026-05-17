#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Models;

namespace CascadeIDE.Cockpit.Graph.Layout;

/// <summary>Связывает <see cref="GraphKind"/> и уровень карты с <see cref="GraphLayoutPresentation"/>.</summary>
public static class GraphLayoutPresentationResolver
{
    public static GraphLayoutPresentation Resolve(GraphDocument doc, string semanticMapLevel)
    {
        if (doc.Kind != GraphKind.Unspecified)
            return ToPresentation(doc.Kind);

        var level = CodeNavigationMapLevelKind.Normalize(semanticMapLevel);
        return string.Equals(level, CodeNavigationMapLevelKind.ControlFlow, StringComparison.Ordinal)
            ? GraphLayoutPresentation.CodeControlFlow
            : GraphLayoutPresentation.WorkspaceRelatedFiles;
    }

    private static GraphLayoutPresentation ToPresentation(GraphKind kind) =>
        kind switch
        {
            GraphKind.CodeIntent => GraphLayoutPresentation.CodeControlFlow,
            GraphKind.RelatedFiles => GraphLayoutPresentation.WorkspaceRelatedFiles,
            GraphKind.RepositoryModuleTree => GraphLayoutPresentation.WorkspaceRelatedFiles,
            _ => GraphLayoutPresentation.WorkspaceRelatedFiles
        };
}
