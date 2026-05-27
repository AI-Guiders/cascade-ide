using Avalonia;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class StarGraphLayoutEngineTests
{
    [Fact]
    public void Layout_PlacesAnchorCenterAndSatellitesOnOrbit()
    {
        var engine = new StarGraphLayoutEngine();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode
                {
                    Id = "n0",
                    Path = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs"
                },
                new GraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\B.cs",
                    Kind = "project_peer",
                    Label = "B.cs"
                },
                new GraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\C.cs",
                    Kind = "project_peer",
                    Label = "C.cs"
                }
            ],
            Edges =
            [
                new GraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" },
                new GraphEdge { FromId = "n0", ToId = "n2", Kind = "related_to" }
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

    [Fact]
    public void Layout_ManySatellites_SpreadsBeyondCompactCenter()
    {
        var engine = new StarGraphLayoutEngine();
        var nodes = new List<GraphNode>
        {
            new()
            {
                Id = "n0",
                Path = @"D:\w\A.cs",
                Kind = "anchor",
                Label = "A.cs"
            }
        };
        for (var i = 1; i <= 14; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = $"n{i}",
                Path = $@"D:\w\F{i}.cs",
                Kind = "project_peer",
                Label = $"File{i}.cs"
            });
        }

        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = nodes.Skip(1).Select(n => new GraphEdge { FromId = "n0", ToId = n.Id, Kind = "related_to" }).ToList()
        };

        var compact = engine.Layout(doc, 400, 120);
        var tall = engine.Layout(doc, 400, 240);
        var maxRadiusCompact = compact.Nodes.Where(n => !n.IsAnchor).Max(n =>
            Math.Sqrt(Math.Pow(n.Center.X - 200, 2) + Math.Pow(n.Center.Y - 60, 2)));
        var maxRadiusTall = tall.Nodes.Where(n => !n.IsAnchor).Max(n =>
            Math.Sqrt(Math.Pow(n.Center.X - 200, 2) + Math.Pow(n.Center.Y - 120, 2)));
        Assert.True(maxRadiusTall > maxRadiusCompact + 8);
    }
}
