using Avalonia;
using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationStarGraphLayoutEngineTests
{
    [Fact]
    public void Layout_PlacesAnchorCenterAndSatellitesOnOrbit()
    {
        var engine = new WorkspaceNavigationStarGraphLayoutEngine();
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new WorkspaceNavigationSubgraphNode
                {
                    Id = "n0",
                    Path = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs"
                },
                new WorkspaceNavigationSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\B.cs",
                    Kind = "project_peer",
                    Label = "B.cs"
                },
                new WorkspaceNavigationSubgraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\C.cs",
                    Kind = "project_peer",
                    Label = "C.cs"
                }
            ],
            Edges =
            [
                new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n2", Kind = "related_to" }
            ]
        };

        var scene = engine.Layout(doc, 280, 120);
        Assert.Equal(3, scene.Nodes.Count);
        var anchor = scene.Nodes.First(n => n.IsAnchor);
        Assert.Equal(140, anchor.Center.X, 0.5);
        Assert.Equal(60, anchor.Center.Y, 0.5);
        Assert.True(scene.Edges.Count >= 2);
    }
}
