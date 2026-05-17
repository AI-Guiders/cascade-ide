#nullable enable
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Views.SkiaKit.Graph;

/// <inheritdoc cref="CodeNavigationMapGraphSceneProjection"/>
public static class GraphLayoutSceneMapper
{
    public static GraphLayoutScene FromViewModel(CodeNavigationMapGraphSceneVm scene) =>
        CodeNavigationMapGraphSceneProjection.FromViewModel(scene);

    public static GraphLayoutNode MapNode(CodeNavigationMapGraphNodeLayout n) =>
        CodeNavigationMapGraphSceneProjection.ToLayoutNode(n);

    public static GraphLayoutEdge MapEdge(CodeNavigationMapGraphEdgeLayout e) =>
        CodeNavigationMapGraphSceneProjection.ToLayoutEdge(e);
}
