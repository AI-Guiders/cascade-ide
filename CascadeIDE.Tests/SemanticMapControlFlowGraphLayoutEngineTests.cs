using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapControlFlowGraphLayoutEngineTests
{
    [Fact]
    public void Layout_PlacesFlowTopToBottom_ByDepth()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode
                {
                    Id = "n0",
                    Path = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs"
                },
                new SemanticMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "B"
                },
                new SemanticMapSubgraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "C"
                }
            ],
            Edges =
            [
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n1", ToId = "n2", Kind = "Call" }
            ]
        };

        var scene = engine.Layout(doc, 280, 120);
        Assert.Equal(3, scene.Nodes.Count);

        var n0 = scene.Nodes.Single(n => n.Id == "n0");
        var n1 = scene.Nodes.Single(n => n.Id == "n1");
        var n2 = scene.Nodes.Single(n => n.Id == "n2");

        Assert.True(n0.IsAnchor);
        Assert.True(n0.Center.Y < n1.Center.Y);
        Assert.True(n1.Center.Y < n2.Center.Y);
        Assert.Equal(140, n0.Center.X, 0.5);
    }

    [Fact]
    public void Layout_PlacesSameDepthBranchesSideBySide()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode
                {
                    Id = "n0",
                    Path = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs"
                },
                new SemanticMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "LeftBranch"
                },
                new SemanticMapSubgraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "RightBranch"
                }
            ],
            Edges =
            [
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "ConditionalCall" },
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n2", Kind = "ConditionalCall" }
            ]
        };

        var scene = engine.Layout(doc, 280, 120);
        var n1 = scene.Nodes.Single(n => n.Id == "n1");
        var n2 = scene.Nodes.Single(n => n.Id == "n2");
        Assert.Equal(n1.Center.Y, n2.Center.Y, 0.5);
        Assert.True(n1.Center.X < n2.Center.X);
    }

    [Fact]
    public void Layout_NarrowSlot_ShortensLabelsAndSetsAdaptiveSideFont()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode
                {
                    Id = "n0",
                    Path = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs"
                },
                new SemanticMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = new string('M', 40)
                }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };

        var scene = engine.Layout(doc, 118, 200);
        Assert.NotNull(scene.SideLabelFontSizePx);
        Assert.InRange(scene.SideLabelFontSizePx!.Value, SemanticMapRenderInvariants.CompactSideLabelFontSizeFloor, SemanticMapRenderInvariants.MaxSideLabelFontSize);
        var n1 = scene.Nodes.Single(n => n.Id == "n1");
        Assert.True(n1.Label.Length < 40);
        Assert.EndsWith("…", n1.Label);
    }

    [Fact]
    public void Layout_WideSlot_AllowsLongerLabelsThanNarrow()
    {
        var narrow = SemanticMapGraphPrimitives.ResolveControlFlowLabelCharBudget(118);
        var wide = SemanticMapGraphPrimitives.ResolveControlFlowLabelCharBudget(360);
        Assert.True(wide > narrow);
    }

    [Fact]
    public void Layout_LegendColumnLeft_StartsAtMaxInkRightPlusGap_NotReadableBandRight()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A" },
                new SemanticMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "condition_step",
                    Label = "IF",
                    LegendIndex = 1,
                    LegendText = "x > 0"
                }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };
        const double viewportW = 400;
        var scene = engine.Layout(doc, viewportW, 200);
        Assert.True(scene.UseLegendColumn);
        var inkSl = SemanticMapGraphPrimitives.ControlFlowBesideLegendInkSlack;
        var minCl = Math.Max(
            SemanticMapGraphPrimitives.ControlFlowLegendGap,
            SemanticMapGraphPrimitives.ControlFlowLegendBesideMinClearance);
        var inkR = 0.0;
        foreach (var n in scene.Nodes)
            inkR = Math.Max(inkR, n.Center.X + n.Radius + inkSl);
        var expected = inkR + minCl;
        Assert.Equal(expected, scene.LegendColumnLeft, 0.6);
    }

    [Fact]
    public void Layout_NarrowWidth_PlacesLegendBlockBelowGraph_FullWidthText()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A" },
                new SemanticMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "condition_step",
                    Label = "IF",
                    LegendIndex = 1,
                    LegendText = "pred"
                }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };
        // Мало места справа от «чернила» — блок легенды уходит вниз на полную ширину.
        var scene = engine.Layout(doc, 130, 200);
        Assert.True(scene.UseLegendColumn);
        Assert.Equal(SemanticMapLegendBlockPlacement.BelowGraph, scene.LegendPlacement);
        Assert.True(scene.LegendBlockTopY > 0);
        Assert.Equal(SemanticMapGraphPrimitives.ControlFlowSidePadding, scene.LegendColumnLeft, 0.01);
    }
}
