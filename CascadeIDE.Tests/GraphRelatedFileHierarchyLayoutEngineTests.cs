using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GraphRelatedFileHierarchyLayoutEngineTests
{
    [Fact]
    public void TopDown_PlacesAnchorAboveChildren()
    {
        var doc = SampleDoc();
        var scene = new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: true)
            .Layout(doc, 320, 220, CodeNavigationMapDetailLevel.Normal);

        var anchor = scene.Nodes.First(n => n.IsAnchor);
        var child = scene.Nodes.First(n => !n.IsAnchor);
        Assert.True(anchor.Center.Y < child.Center.Y);
        Assert.Equal(CodeNavigationMapRelatedGraphLayoutKind.TopDown, scene.RelatedFilesLayout);
    }

    [Fact]
    public void BottomUp_PlacesAnchorBelowChildren()
    {
        var doc = SampleDoc();
        var scene = new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: false)
            .Layout(doc, 320, 220, CodeNavigationMapDetailLevel.Normal);

        var anchor = scene.Nodes.First(n => n.IsAnchor);
        var child = scene.Nodes.First(n => !n.IsAnchor);
        Assert.True(anchor.Center.Y > child.Center.Y);
    }

    private static GraphDocument SampleDoc() =>
        new()
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode { Id = "n1", Path = @"D:\w\B.cs", Kind = "project_peer", Label = "B.cs" },
                new GraphNode { Id = "n2", Path = @"D:\w\C.cs", Kind = "project_peer", Label = "C.cs" }
            ],
            Edges =
            [
                new GraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" },
                new GraphEdge { FromId = "n0", ToId = "n2", Kind = "related_to" }
            ]
        };
}
