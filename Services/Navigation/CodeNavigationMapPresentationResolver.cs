#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Services.Navigation;

/// <summary>Мост layout presentation → VM enum (binding).</summary>
public static class CodeNavigationMapPresentationResolver
{
    public static CodeNavigationMapGraphPresentationKind Resolve(GraphDocument doc, string semanticMapLevel)
    {
        var layout = GraphLayoutPresentationResolver.Resolve(doc, semanticMapLevel);
        return layout == GraphLayoutPresentation.WorkspaceRelatedFiles
            ? CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles
            : CodeNavigationMapGraphPresentationKind.CodeControlFlow;
    }
}
