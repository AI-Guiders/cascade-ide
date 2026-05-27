using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services.SkiaInstruments;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapCompositorTests
{
    [Fact]
    public void Compose_ControlFlow_ExpandsPreferredHeightForLongFlow()
    {
        var compositor = new CodeNavigationMapCompositor();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" },
                new GraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S2" },
                new GraphNode { Id = "n3", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S3" },
                new GraphNode { Id = "n4", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S4" },
                new GraphNode { Id = "n5", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S5" }
            ],
            Edges =
            [
                new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new GraphEdge { FromId = "n1", ToId = "n2", Kind = "Call" },
                new GraphEdge { FromId = "n2", ToId = "n3", Kind = "Call" },
                new GraphEdge { FromId = "n3", ToId = "n4", Kind = "Call" },
                new GraphEdge { FromId = "n4", ToId = "n5", Kind = "Call" }
            ]
        };

        var result = compositor.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 280, 120);
        Assert.True(result.PreferredHeight >= CodeNavigationMapCompositor.DefaultHeightControlFlow);
        Assert.True(result.PreferredHeight <= CodeNavigationMapCompositor.MaxHeightControlFlow);
        var sceneVm = result.ToSceneVm(280, result.PreferredHeight);
        Assert.Equal(doc.Nodes.Count, sceneVm.Nodes.Count);
        Assert.Equal(CodeNavigationMapGraphPresentationKind.CodeControlFlow, sceneVm.Presentation);
    }

    [Fact]
    public void Compose_ControlFlow_LongFlow_CapsPreferredHeight()
    {
        var compositor = new CodeNavigationMapCompositor();
        var nodes = new List<GraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        var edges = new List<GraphEdge>();
        for (var i = 1; i <= 25; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = $"n{i}",
                Path = @"D:\w\A.cs",
                Kind = "call_step",
                Label = $"S{i}"
            });
            edges.Add(new GraphEdge { FromId = $"n{i - 1}", ToId = $"n{i}", Kind = "Call" });
        }

        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = edges
        };

        var result = compositor.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 280, 120);
        Assert.Equal(CodeNavigationMapCompositor.MaxHeightControlFlow, result.PreferredHeight);
    }

    [Fact]
    public void Compose_ControlFlow_TallViewport_KeepsIntrinsicPreferredHeightAndReadableStep()
    {
        var compositor = new CodeNavigationMapCompositor();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };

        var result = compositor.Compose(
            new CodeNavigationMapCompositionIntent(
                doc,
                CodeNavigationMapLevelKind.ControlFlow,
                CodeNavigationMapDetailLevel.Normal,
                CodeNavigationMapRelatedGraphLayoutKind.Radial,
                CodeNavigationMapControlFlowMainAxisKind.Vertical),
            new SkiaInstrumentViewport(280, 420));
        Assert.True(result.PreferredHeight < 350);
        var sceneVm = result.ToSceneVm(280, result.PreferredHeight);
        var n0 = sceneVm.Nodes.First(n => n.Id == "n0");
        var n1 = sceneVm.Nodes.First(n => n.Id == "n1");
        var dy = Math.Abs(n1.Center.Y - n0.Center.Y);
        // Высокий viewport разрешает увеличенный вертикальный шаг (см. ControlFlowMaxReadableVerticalStepCap).
        Assert.InRange(dy, 18, 96);
    }

    [Fact]
    public void ControlFlowLayout_WideGraphArea_CentersReadableBand()
    {
        var engine = new ControlFlowGraphLayoutEngine();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "condition_step", Label = "L", LegendIndex = 1, LegendText = "a" },
                new GraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "condition_step", Label = "R", LegendIndex = 2, LegendText = "b" }
            ],
            Edges =
            [
                new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new GraphEdge { FromId = "n0", ToId = "n2", Kind = "Call" }
            ]
        };

        var scene = engine.Layout(doc, 920, 280);
        var n1 = scene.Nodes.First(n => n.Id == "n1");
        var n2 = scene.Nodes.First(n => n.Id == "n2");
        Assert.True(Math.Abs(n2.Center.X - n1.Center.X) < 400);
    }

    [Fact]
    public void Compose_File_SmallGraph_KeepsCompactHeight()
    {
        var compositor = new CodeNavigationMapCompositor();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode { Id = "n1", Path = @"D:\w\B.cs", Kind = "project_peer", Label = "B.cs" }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" }]
        };

        var result = compositor.Compose(doc, CodeNavigationMapLevelKind.File, 280, 120);
        Assert.Equal(120, result.PreferredHeight, 0.1);
        var sceneVm = result.ToSceneVm(280, result.PreferredHeight);
        Assert.Equal(2, sceneVm.Nodes.Count);
        Assert.Equal(CodeNavigationMapGraphPresentationKind.WorkspaceRelatedFiles, sceneVm.Presentation);
    }

    [Fact]
    public void Compose_File_DenseGraph_ExpandsPreferredHeight()
    {
        var compositor = new CodeNavigationMapCompositor();
        var nodes = new List<GraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        for (var i = 1; i <= 14; i++)
            nodes.Add(new GraphNode { Id = $"n{i}", Path = $@"D:\w\B{i}.cs", Kind = "project_peer", Label = $"B{i}.cs" });

        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = nodes.Skip(1).Select(n => new GraphEdge { FromId = "n0", ToId = n.Id, Kind = "related_to" }).ToList()
        };

        var result = compositor.Compose(
            new CodeNavigationMapCompositionIntent(doc, CodeNavigationMapLevelKind.File, CodeNavigationMapDetailLevel.Inspect),
            new SkiaInstrumentViewport(400, 120));
        Assert.True(result.PreferredHeight > 120);
        Assert.True(result.PreferredHeight <= GraphFileLayoutMetrics.MaxHeightFile);
    }

    [Fact]
    public void Compose_GenericPipelineEntryPoint_ProducesScene()
    {
        var compositor = new CodeNavigationMapCompositor();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = [new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }],
            Edges = []
        };

        var result = compositor.Compose(
            new CodeNavigationMapCompositionIntent(doc, CodeNavigationMapLevelKind.File),
            new SkiaInstrumentViewport(280, 120));
        Assert.Single(result.ToSceneVm(280, result.PreferredHeight).Nodes);
    }

    [Fact]
    public void IntentStage_DetectsLoopEdgeMetrics()
    {
        var stage = new CodeNavigationMapIntentStage();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "B" }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "LoopCall" }]
        };

        var state = stage.Resolve(new CodeNavigationMapPipelineContext(
            doc,
            CodeNavigationMapLevelKind.ControlFlow,
            new SkiaInstrumentViewport(280, 120)));

        Assert.Equal(1, state.LoopEdgeCount);
        Assert.Equal(CodeNavigationMapLevelKind.ControlFlow, state.MapLevel);
    }

    [Fact]
    public void Compose_ControlFlow_Glance_FiltersMultibranchOnlyBranch()
    {
        var compositor = new CodeNavigationMapCompositor();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A" },
                new GraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" },
                new GraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S2" }
            ],
            Edges =
            [
                new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new GraphEdge { FromId = "n0", ToId = "n2", Kind = "multibranch" }
            ]
        };

        var glance = compositor.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 280, 120, CodeNavigationMapDetailLevel.Glance);
        var inspect = compositor.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 280, 120, CodeNavigationMapDetailLevel.Inspect);
        Assert.Equal(2, glance.ToSceneVm(280, glance.PreferredHeight).Nodes.Count);
        Assert.Equal(3, inspect.ToSceneVm(280, inspect.PreferredHeight).Nodes.Count);
    }

    [Fact]
    public void Compose_ControlFlow_GlanceVsInspect_PreferredHeightInspectIsTallerWhenIntrinsicExceedsMinClamp()
    {
        var compositor = new CodeNavigationMapCompositor();
        var nodes = new List<GraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        var edges = new List<GraphEdge>();
        for (var i = 1; i <= 17; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = $"n{i}",
                Path = @"D:\w\A.cs",
                Kind = "call_step",
                Label = $"S{i}"
            });
            edges.Add(new GraphEdge { FromId = $"n{i - 1}", ToId = $"n{i}", Kind = "Call" });
        }

        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = edges
        };

        var glance = compositor.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 280, 120, CodeNavigationMapDetailLevel.Glance);
        var inspect = compositor.Compose(doc, CodeNavigationMapLevelKind.ControlFlow, 280, 120, CodeNavigationMapDetailLevel.Inspect);
        Assert.True(inspect.PreferredHeight > glance.PreferredHeight);
    }

    [Fact]
    public void ControlFlowLayout_WithLegend_ReservesColumnAndConditionBranch()
    {
        var engine = new ControlFlowGraphLayoutEngine();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "condition_step",
                    Label = "IF",
                    LegendIndex = 1,
                    LegendText = "x > 0"
                }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };

        var scene = engine.Layout(doc, 400, 200, CodeNavigationMapDetailLevel.Inspect);
        Assert.Single(scene.Legend);
        Assert.Equal(1, scene.Legend[0].Index);
        Assert.Equal("x > 0", scene.Legend[0].Text);
        Assert.True(scene.UseLegendColumn);
        Assert.True(scene.ShowLegendConditionKey);
        Assert.False(scene.ShowLegendReturnKey);
        Assert.True(scene.LegendColumnLeft < 400);
        var cond = scene.Nodes.First(n => n.Id == "n1");
        Assert.Equal(GraphNodeShape.Condition, cond.Shape);
    }

    [Fact]
    public void ControlFlowLayout_SkipsReturnInLegendRows_ShowsReturnShapeKey()
    {
        var engine = new ControlFlowGraphLayoutEngine();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "exit_step",
                    Label = "RET",
                    LegendIndex = 1,
                    LegendText = "return"
                }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Exit" }]
        };

        var scene = engine.Layout(doc, 400, 200, CodeNavigationMapDetailLevel.Normal);
        Assert.Empty(scene.Legend);
        Assert.True(scene.UseLegendColumn);
        Assert.True(scene.ShowLegendReturnKey);
        Assert.False(scene.ShowLegendConditionKey);
        Assert.True(scene.LegendColumnLeft < 400);
    }

    [Fact]
    public void ControlFlowLayout_Normal_UsesGlyphsWithoutLegendColumn()
    {
        var engine = new ControlFlowGraphLayoutEngine();
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new GraphNode
                {
                    Id = "n1",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "Run",
                    LegendIndex = 1,
                    LegendText = "await Run();",
                    LineStart = 10,
                    LineEnd = 10
                }
            ],
            Edges = [new GraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" }]
        };

        var scene = engine.Layout(doc, 400, 200, CodeNavigationMapDetailLevel.Normal);
        Assert.Empty(scene.Legend);
        Assert.False(scene.UseLegendColumn);
        Assert.True(scene.ShowNodeLegendGlyphs);
        Assert.Equal(10, scene.Nodes.First(n => n.Id == "n1").LineStart);
    }

    [Fact]
    public void Compose_ControlFlow_IntentForcesVertical_OverWideShortViewport()
    {
        var compositor = new CodeNavigationMapCompositor();
        GraphNode Node(string id, string label) =>
            new()
            {
                Id = id,
                Path = @"D:\w\Chain.cs",
                Kind = "call_step",
                Label = label
            };

        var edges = new List<GraphEdge>();
        var nodes = new List<GraphNode>
        {
            new()
            {
                Id = "n0",
                Path = @"D:\w\Chain.cs",
                Kind = "anchor",
                Label = "Anchor"
            }
        };
        for (var i = 1; i <= 6; i++)
        {
            nodes.Add(Node($"n{i}", $"s{i}"));
            edges.Add(new GraphEdge { FromId = $"n{i - 1}", ToId = $"n{i}", Kind = "Call" });
        }

        var doc = new GraphDocument { AnchorPath = @"D:\w\Chain.cs", Nodes = nodes, Edges = edges };
        var intent = new CodeNavigationMapCompositionIntent(
            doc,
            CodeNavigationMapLevelKind.ControlFlow,
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Vertical);

        var result = compositor.Compose(intent, new SkiaInstrumentViewport(640, 130));
        Assert.Equal(GraphControlFlowMainAxis.Vertical, result.LayoutScene.ControlFlowMainAxis);
    }

    [Fact]
    public void SubgraphJson_ParsesLegendFields()
    {
        const string json =
            """{"mode":"subgraph","graph_kind":"code_intent_code_navigation_map","anchor_path":"D:\\a.cs","nodes":[{"id":"n0","path":"D:\\a.cs","kind":"anchor","label":"a.cs","relative_path":"","rationale":""},{"id":"n1","path":"D:\\a.cs","kind":"condition_step","label":"IF","relative_path":"","rationale":"","legend_index":1,"legend_text":"x > 0"}],"edges":[]}""";
        Assert.True(GraphDocumentJson.TryParse(json, out var doc, out _));
        Assert.NotNull(doc);
        Assert.Equal(GraphKind.CodeIntent, doc!.Kind);
        var n1 = doc.Nodes.First(n => n.Id == "n1");
        Assert.Equal(1, n1.LegendIndex);
        Assert.Equal("x > 0", n1.LegendText);
    }
}
