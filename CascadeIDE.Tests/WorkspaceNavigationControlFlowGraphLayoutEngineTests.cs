using CascadeIDE.Services;
using CascadeIDE.Services.Navigation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationControlFlowGraphLayoutEngineTests
{
    [Fact]
    public void Layout_PlacesFlowTopToBottom_ByDepth()
    {
        var engine = new WorkspaceNavigationControlFlowGraphLayoutEngine();
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
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "B"
                },
                new WorkspaceNavigationSubgraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "C"
                }
            ],
            Edges =
            [
                new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "Call" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n1", ToId = "n2", Kind = "Call" }
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
        var engine = new WorkspaceNavigationControlFlowGraphLayoutEngine();
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
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "LeftBranch"
                },
                new WorkspaceNavigationSubgraphNode
                {
                    Id = "n2",
                    Path = @"D:\w\A.cs",
                    Kind = "call_step",
                    Label = "RightBranch"
                }
            ],
            Edges =
            [
                new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n1", Kind = "ConditionalCall" },
                new WorkspaceNavigationSubgraphEdge { FromId = "n0", ToId = "n2", Kind = "ConditionalCall" }
            ]
        };

        var scene = engine.Layout(doc, 280, 120);
        var n1 = scene.Nodes.Single(n => n.Id == "n1");
        var n2 = scene.Nodes.Single(n => n.Id == "n2");
        Assert.Equal(n1.Center.Y, n2.Center.Y, 0.5);
        Assert.True(n1.Center.X < n2.Center.X);
    }
}
