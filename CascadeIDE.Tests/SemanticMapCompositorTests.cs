using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using CascadeIDE.Services.SkiaInstruments;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SemanticMapCompositorTests
{
    [Fact]
    public void Compose_ControlFlow_ExpandsPreferredHeightForLongFlow()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new WorkspaceNavigationSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new WorkspaceNavigationSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S1" },
                new WorkspaceNavigationSubgraphNode { Id = "n2", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S2" },
                new WorkspaceNavigationSubgraphNode { Id = "n3", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S3" },
                new WorkspaceNavigationSubgraphNode { Id = "n4", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S4" },
                new WorkspaceNavigationSubgraphNode { Id = "n5", Path = @"D:\w\A.cs", Kind = "call_step", Label = "S5" }
            ],
            Edges =
            [
                new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n1", ToId = "n2", Kind = "Call" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n2", ToId = "n3", Kind = "Call" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n3", ToId = "n4", Kind = "Call" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n4", ToId = "n5", Kind = "Call" }
            ]
        };

        var result = compositor.Compose(doc, CascadeIDE.Models.SemanticMapLevelKind.ControlFlow, 280, 120);
        Assert.True(result.PreferredHeight >= SemanticMapCompositor.DefaultHeightControlFlow);
        Assert.Equal(doc.Nodes.Count, result.Scene.Nodes.Count);
    }

    [Fact]
    public void Compose_File_KeepsCompactHeight()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new WorkspaceNavigationSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new WorkspaceNavigationSubgraphNode { Id = "n1", Path = @"D:\w\B.cs", Kind = "project_peer", Label = "B.cs" }
            ],
            Edges = [new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "related_to" }]
        };

        var result = compositor.Compose(doc, CascadeIDE.Models.SemanticMapLevelKind.File, 280, 120);
        Assert.Equal(120, result.PreferredHeight, 0.1);
        Assert.Equal(2, result.Scene.Nodes.Count);
    }

    [Fact]
    public void Compose_GenericPipelineEntryPoint_ProducesScene()
    {
        var compositor = new SemanticMapCompositor();
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = [new WorkspaceNavigationSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }],
            Edges = []
        };

        var result = compositor.Compose(
            new SemanticMapCompositionIntent(doc, CascadeIDE.Models.SemanticMapLevelKind.File),
            new SkiaInstrumentViewport(280, 120));
        Assert.Single(result.Scene.Nodes);
    }

    [Fact]
    public void IntentStage_DetectsLoopEdgeMetrics()
    {
        var stage = new SemanticMapIntentStage();
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new WorkspaceNavigationSubgraphNode { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" },
                new WorkspaceNavigationSubgraphNode { Id = "n1", Path = @"D:\w\A.cs", Kind = "call_step", Label = "B" }
            ],
            Edges = [new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "LoopCall" }]
        };

        var state = stage.Resolve(new SemanticMapPipelineContext(
            doc,
            CascadeIDE.Models.SemanticMapLevelKind.ControlFlow,
            new SkiaInstrumentViewport(280, 120)));

        Assert.Equal(1, state.LoopEdgeCount);
        Assert.Equal(CascadeIDE.Models.SemanticMapLevelKind.ControlFlow, state.SemanticMapLevel);
    }
}
