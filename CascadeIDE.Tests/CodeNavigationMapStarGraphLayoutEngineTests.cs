using Avalonia;
using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapStarGraphLayoutEngineTests
{
    [Fact]
    public void Layout_PlacesAnchorCenterAndSatellitesOnOrbit()
    {
        var engine = new CodeNavigationMapStarGraphLayoutEngine();
        var doc = new CodeNavigationMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new CodeNavigationMapSubgraphNode
                {
                    Id = "n0",
                    Path = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs"
                },
                new CodeNavigationMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\B.cs",
                    Kind = "project_peer",
                    Label = "B.cs"
                },
                new CodeNavigationMapSubgraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\C.cs",
                    Kind = "project_peer",
                    Label = "C.cs"
                }
            ],
            Edges =
            [
                new CodeNavigationMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" },
                new CodeNavigationMapSubgraphEdge { FromId = "n0", ToId = "n2", Kind = "related_to" }
            ]
        };

        var scene = engine.Layout(doc, 280, 120);
        Assert.Equal(3, scene.Nodes.Count);
        var anchor = scene.Nodes.First(n => n.IsAnchor);
        Assert.Equal(140, anchor.Center.X, 0.5);
        Assert.Equal(60, anchor.Center.Y, 0.5);
        Assert.True(scene.Edges.Count >= 2);
        Assert.All(scene.Edges, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.FromNodeId));
            Assert.False(string.IsNullOrWhiteSpace(e.ToNodeId));
            Assert.True(e.ToRadius > 0);
        });
        Assert.Contains(scene.Edges, e => string.Equals(e.Kind, "related_to", StringComparison.Ordinal));
    }
}
