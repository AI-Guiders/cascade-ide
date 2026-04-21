using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapCompositorTests
{
    [Fact]
    public void Compose_ControlFlow_ExpandsPreferredHeightForLongFlow()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new SemanticMapSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" },
                new SemanticMapSubgraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S2" },
                new SemanticMapSubgraphNode { Id = "n3", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S3" },
                new SemanticMapSubgraphNode { Id = "n4", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S4" },
                new SemanticMapSubgraphNode { Id = "n5", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S5" }
            ],
            Edges =
            [
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n1", ToId = "n2", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n2", ToId = "n3", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n3", ToId = "n4", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n4", ToId = "n5", Kind = "Call" }
            ]
        };

        var result = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 120);
        Assert.True(result.PreferredHeight >= SemanticMapCompositor.DefaultHeightControlFlow);
        Assert.True(result.PreferredHeight <= SemanticMapCompositor.MaxHeightControlFlow);
        Assert.Equal(doc.Nodes.Count, result.Scene.Nodes.Count);
        Assert.Equal(SemanticMapGraphPresentationKind.CodeControlFlow, result.Scene.Presentation);
    }

    [Fact]
    public void Compose_ControlFlow_LongFlow_CapsPreferredHeight()
    {
        var compositor = new SemanticMapCompositor();
        var nodes = new List<SemanticMapSubgraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        var edges = new List<SemanticMapSubgraphEdge>();
        for (var i = 1; i <= 25; i++)
        {
            nodes.Add(new SemanticMapSubgraphNode
            {
                Id = $"n{i}",
                Path = @"D:\w\A.cs",
                Kind = "call_step",
                Label = $"S{i}"
            });
            edges.Add(new SemanticMapSubgraphEdge { FromId = $"n{i - 1}", ToId = $"n{i}", Kind = "Call" });
        }

        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = edges
        };

        var result = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 120);
        Assert.Equal(SemanticMapCompositor.MaxHeightControlFlow, result.PreferredHeight);
    }

    [Fact]
    public void Compose_ControlFlow_TallViewport_KeepsIntrinsicPreferredHeightAndReadableStep()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new SemanticMapSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };

        var result = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 420);
        Assert.True(result.PreferredHeight < 350);
        var n0 = result.Scene.Nodes.First(n => n.Id == "n0");
        var n1 = result.Scene.Nodes.First(n => n.Id == "n1");
        var dy = Math.Abs(n1.Center.Y - n0.Center.Y);
        // Высокий viewport разрешает увеличенный вертикальный шаг (см. ControlFlowMaxReadableVerticalStepCap).
        Assert.InRange(dy, 18, 95);
    }

    [Fact]
    public void ControlFlowLayout_WideGraphArea_CentersReadableBand()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new SemanticMapSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "condition_step", Label = "L", LegendIndex = 1, LegendText = "a" },
                new SemanticMapSubgraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "condition_step", Label = "R", LegendIndex = 2, LegendText = "b" }
            ],
            Edges =
            [
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n2", Kind = "Call" }
            ]
        };

        var scene = engine.Layout(doc, 920, 280);
        var n1 = scene.Nodes.First(n => n.Id == "n1");
        var n2 = scene.Nodes.First(n => n.Id == "n2");
        Assert.True(Math.Abs(n2.Center.X - n1.Center.X) < 400);
    }

    [Fact]
    public void Compose_File_KeepsCompactHeight()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new SemanticMapSubgraphNode { Id = "n1", Path = @"D:\w\B.cs", Kind = "project_peer", Label = "B.cs" }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" }]
        };

        var result = compositor.Compose(doc, SemanticMapLevelKind.File, 280, 120);
        Assert.Equal(120, result.PreferredHeight, 0.1);
        Assert.Equal(2, result.Scene.Nodes.Count);
        Assert.Equal(SemanticMapGraphPresentationKind.WorkspaceRelatedFiles, result.Scene.Presentation);
    }

    [Fact]
    public void Compose_GenericPipelineEntryPoint_ProducesScene()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = [new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }],
            Edges = []
        };

        var result = compositor.Compose(
            new SemanticMapCompositionIntent(doc, SemanticMapLevelKind.File),
            new SkiaInstrumentViewport(280, 120));
        Assert.Single(result.Scene.Nodes);
    }

    [Fact]
    public void IntentStage_DetectsLoopEdgeMetrics()
    {
        var stage = new SemanticMapIntentStage();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new SemanticMapSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "B" }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "LoopCall" }]
        };

        var state = stage.Resolve(new SemanticMapPipelineContext(
            doc,
            SemanticMapLevelKind.ControlFlow,
            new SkiaInstrumentViewport(280, 120)));

        Assert.Equal(1, state.LoopEdgeCount);
        Assert.Equal(SemanticMapLevelKind.ControlFlow, state.SemanticMapLevel);
    }

    [Fact]
    public void Compose_ControlFlow_Glance_FiltersMultibranchOnlyBranch()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A" },
                new SemanticMapSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" },
                new SemanticMapSubgraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S2" }
            ],
            Edges =
            [
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n2", Kind = "multibranch" }
            ]
        };

        var glance = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 120, SemanticMapDetailLevel.Glance);
        var inspect = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 120, SemanticMapDetailLevel.Inspect);
        Assert.Equal(2, glance.Scene.Nodes.Count);
        Assert.Equal(3, inspect.Scene.Nodes.Count);
    }

    [Fact]
    public void Compose_ControlFlow_GlanceVsInspect_PreferredHeightInspectIsTallerWhenIntrinsicExceedsMinClamp()
    {
        var compositor = new SemanticMapCompositor();
        var nodes = new List<SemanticMapSubgraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        var edges = new List<SemanticMapSubgraphEdge>();
        for (var i = 1; i <= 17; i++)
        {
            nodes.Add(new SemanticMapSubgraphNode
            {
                Id = $"n{i}",
                Path = @"D:\w\A.cs",
                Kind = "call_step",
                Label = $"S{i}"
            });
            edges.Add(new SemanticMapSubgraphEdge { FromId = $"n{i - 1}", ToId = $"n{i}", Kind = "Call" });
        }

        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = edges
        };

        var glance = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 120, SemanticMapDetailLevel.Glance);
        var inspect = compositor.Compose(doc, SemanticMapLevelKind.ControlFlow, 280, 120, SemanticMapDetailLevel.Inspect);
        Assert.True(inspect.PreferredHeight > glance.PreferredHeight);
    }

    [Fact]
    public void ControlFlowLayout_WithLegend_ReservesColumnAndConditionBranch()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
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

        var scene = engine.Layout(doc, 400, 200);
        Assert.Single(scene.Legend);
        Assert.Equal(1, scene.Legend[0].Index);
        Assert.Equal("x > 0", scene.Legend[0].Text);
        Assert.True(scene.UseLegendColumn);
        Assert.True(scene.ShowLegendConditionKey);
        Assert.False(scene.ShowLegendReturnKey);
        Assert.True(scene.LegendColumnLeft < 400);
        var cond = scene.Nodes.First(n => n.Id == "n1");
        Assert.Equal(SemanticMapNodeShape.Condition, cond.Shape);
    }

    [Fact]
    public void ControlFlowLayout_SkipsReturnInLegendRows_ShowsReturnShapeKey()
    {
        var engine = new SemanticMapControlFlowGraphLayoutEngine();
        var doc = new SemanticMapSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new SemanticMapSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new SemanticMapSubgraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "exit_step",
                    Label = "RET",
                    LegendIndex = 1,
                    LegendText = "return"
                }
            ],
            Edges = [new SemanticMapSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Exit" }]
        };

        var scene = engine.Layout(doc, 400, 200);
        Assert.Empty(scene.Legend);
        Assert.True(scene.UseLegendColumn);
        Assert.True(scene.ShowLegendReturnKey);
        Assert.False(scene.ShowLegendConditionKey);
        Assert.True(scene.LegendColumnLeft < 400);
    }

    [Fact]
    public void SubgraphJson_ParsesLegendFields()
    {
        const string json =
            """{"mode":"subgraph","graph_kind":"code_intent_semantic_map","anchor_path":"D:\\a.cs","nodes":[{"id":"n0","path":"D:\\a.cs","kind":"anchor","label":"a.cs","relative_path":"","rationale":""},{"id":"n1","path":"D:\\a.cs","kind":"condition_step","label":"IF","relative_path":"","rationale":"","legend_index":1,"legend_text":"x > 0"}],"edges":[]}""";
        Assert.True(SemanticMapSubgraphJson.TryParse(json, out var doc, out _));
        Assert.NotNull(doc);
        Assert.Equal(SemanticMapGraphKind.CodeIntentSemanticMap, doc!.GraphKind);
        var n1 = doc.Nodes.First(n => n.Id == "n1");
        Assert.Equal(1, n1.LegendIndex);
        Assert.Equal("x > 0", n1.LegendText);
    }
}
