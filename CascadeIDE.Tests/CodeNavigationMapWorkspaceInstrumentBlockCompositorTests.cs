using Avalonia;
using CascadeIDE.Models;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Services.CodeNavigation;
using CascadeIDE.Services.Navigation;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapWorkspaceInstrumentBlockCompositorTests
{
    [Fact]
    public void Empty_Scene_Yields_No_Blocks()
    {
        var scene = new CodeNavigationMapGraphSceneVm { Nodes = [], Edges = [] };
        var blocks = CodeNavigationMapWorkspaceInstrumentBlockCompositor.Compose(scene, 400, 300);
        Assert.Empty(blocks);
    }

    [Fact]
    public void File_Mode_No_Legend_One_Graph_Block_Full_Viewport()
    {
        var n = new CodeNavigationMapGraphNodeLayout
        {
            Id = "a",
            Kind = "anchor",
            FullPath = "x",
            Label = "a",
            Center = new Point(50, 50),
            Radius = 10,
            IsAnchor = true
        };
        var scene = new CodeNavigationMapGraphSceneVm
        {
            Nodes = [n],
            Edges = [],
            UseLegendColumn = false,
            LegendColumnLeft = 400
        };
        var blocks = CodeNavigationMapWorkspaceInstrumentBlockCompositor.Compose(scene, 400, 300);
        Assert.Single(blocks);
        Assert.Equal(CodeNavigationMapWorkspaceInstrumentBlockIds.Graph, blocks[0].Id);
        Assert.Equal(new Rect(0, 0, 400, 300), blocks[0].Bounds);
    }

    [Fact]
    public void ControlFlow_Beside_Legend_Splits_Horizontally()
    {
        var scene = new CodeNavigationMapGraphSceneVm
        {
            Nodes = [TestNode("n0", 10, 10)],
            Edges = [],
            UseLegendColumn = true,
            LegendColumnLeft = 220,
            LegendPlacement = CodeNavigationMapLegendBlockPlacement.BesideGraph
        };
        var blocks = CodeNavigationMapWorkspaceInstrumentBlockCompositor.Compose(scene, 400, 300);
        Assert.Equal(2, blocks.Count);
        Assert.Equal(CodeNavigationMapWorkspaceInstrumentBlockKind.Graph, blocks[0].Kind);
        Assert.Equal(CodeNavigationMapWorkspaceInstrumentBlockKind.Legend, blocks[1].Kind);
        Assert.Equal(new Rect(0, 0, 220, 300), blocks[0].Bounds);
        Assert.Equal(new Rect(220, 0, 180, 300), blocks[1].Bounds);
    }

    [Fact]
    public void ControlFlow_Below_Legend_Splits_Vertically()
    {
        var scene = new CodeNavigationMapGraphSceneVm
        {
            Nodes = [TestNode("n0", 10, 10)],
            Edges = [],
            UseLegendColumn = true,
            LegendColumnLeft = 0,
            LegendPlacement = CodeNavigationMapLegendBlockPlacement.BelowGraph,
            LegendBlockTopY = 180
        };
        var blocks = CodeNavigationMapWorkspaceInstrumentBlockCompositor.Compose(scene, 400, 400);
        Assert.Equal(2, blocks.Count);
        Assert.Equal(new Rect(0, 0, 400, 180), blocks[0].Bounds);
        Assert.Equal(new Rect(0, 180, 400, 220), blocks[1].Bounds);
    }

    [Fact]
    public void Compositor_Integration_ControlFlow_Produces_Two_Blocks()
    {
        var c = new CodeNavigationMapCompositor();
        var doc = new GraphDocument
        {
            AnchorPath = "A.cs",
            Nodes =
            [
                new GraphNode
                {
                    Id = "n0",
                    Path = "A.cs",
                    Kind = "anchor",
                    Label = "A"
                },
                new GraphNode
                {
                    Id = "n1",
                    Path = "A.cs",
                    Kind = "call_step",
                    Label = "S1",
                    LegendIndex = 1,
                    LegendText = "first line"
                }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };
        var r = c.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 500, 400);
        Assert.NotEmpty(r.CodeNavigationMapInstrumentBlocks);
        Assert.Equal(2, r.CodeNavigationMapInstrumentBlocks.Count);
        Assert.Contains(r.CodeNavigationMapInstrumentBlocks, b => b.Id == CodeNavigationMapInstrumentBlockIds.Graph);
        Assert.Contains(r.CodeNavigationMapInstrumentBlocks, b => b.Id == CodeNavigationMapInstrumentBlockIds.Legend);
    }

    private static CodeNavigationMapGraphNodeLayout TestNode(string id, double x, double y) =>
        new()
        {
            Id = id,
            Kind = "call_step",
            FullPath = "p",
            Label = id,
            Center = new Point(x, y),
            Radius = 8,
            IsAnchor = false
        };
}
