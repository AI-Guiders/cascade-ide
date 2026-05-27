#nullable enable
using Avalonia;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Views.SkiaKit.Graph;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SkiaGraphSceneHitTestingTests
{
    [Fact]
    public void FindNodeAt_prefers_topmost_when_nodes_overlap()
    {
        var scene = new GraphLayoutScene
        {
            Nodes =
            [
                new GraphLayoutNode
                {
                    Id = "back",
                    Kind = "call_step",
                    FullPath = "a.cs",
                    Label = "A",
                    Center = new Point(50, 50),
                    Radius = 20,
                    IsAnchor = false
                },
                new GraphLayoutNode
                {
                    Id = "front",
                    Kind = "call_step",
                    FullPath = "b.cs",
                    Label = "B",
                    Center = new Point(52, 52),
                    Radius = 20,
                    IsAnchor = false
                }
            ],
            Edges = [],
            Legend = [],
            LegendColumnLeft = 200
        };

        var hit = SkiaGraphSceneHitTesting.FindNodeAt(scene, new Point(52, 52));
        Assert.NotNull(hit);
        Assert.Equal("front", hit!.Id);
    }

    [Fact]
    public void MapControlPointToLayout_scales_when_control_smaller_than_layout()
    {
        var mapped = SkiaGraphSceneHitTesting.MapControlPointToLayout(
            new Point(50, 100),
            controlWidth: 100,
            controlHeight: 200,
            layoutViewportWidth: 200,
            layoutViewportHeight: 400);

        Assert.Equal(100, mapped.X, 3);
        Assert.Equal(200, mapped.Y, 3);
    }
}
