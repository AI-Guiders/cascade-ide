using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class UnitTestTraceChannelTests
{
    [Fact]
    public void Build_WhenImpactedTestsPresent_HighlightsExitFlow()
    {
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                Node("n0", "anchor"),
                Node("n1", "condition_step"),
                Node("n2", "exit_step"),
                Node("n3", "call_step")
            ],
            Edges =
            [
                Edge("n0", "n1", "ConditionalCall"),
                Edge("n1", "n2", "Exit"),
                Edge("n1", "n3", "Call")
            ]
        };

        var channel = new UnitTestTraceChannel();
        var snapshot = channel.Build(new TraceFlowChannelContext(doc, ImpactedTestsBadge: 2, LastTestSummary: "1 failed"));

        Assert.Contains("n2", snapshot.HighlightedNodeIds);
        Assert.Contains("n1->n2", snapshot.HighlightedEdgeKeys);
    }

    [Fact]
    public void Build_WhenNoImpactedTests_DoesNotHighlight()
    {
        var doc = new WorkspaceNavigationSubgraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = [Node("n0", "anchor"), Node("n1", "exit_step")],
            Edges = [Edge("n0", "n1", "Exit")]
        };

        var channel = new UnitTestTraceChannel();
        var snapshot = channel.Build(new TraceFlowChannelContext(doc, ImpactedTestsBadge: 0, LastTestSummary: ""));

        Assert.Empty(snapshot.HighlightedNodeIds);
        Assert.Empty(snapshot.HighlightedEdgeKeys);
    }

    private static WorkspaceNavigationSubgraphNode Node(string id, string kind) => new()
    {
        Id = id,
        Path = @"D:\w\A.cs",
        Kind = kind,
        Label = id
    };

    private static WorkspaceNavigationSubgraphEdge Edge(string from, string to, string kind) => new()
    {
        FromId = from,
        ToId = to,
        Kind = kind,
        RelatedKind = kind
    };
}
