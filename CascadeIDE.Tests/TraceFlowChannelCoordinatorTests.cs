using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class TraceFlowChannelCoordinatorTests
{
    [Fact]
    public void Build_MergesSnapshotsFromMultipleChannels()
    {
        var doc = new SemanticMapSubgraphDocument
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

        var coordinator = new TraceFlowChannelCoordinator(
            [
                new CodeFlowTraceChannel(),
                new UnitTestTraceChannel()
            ]);
        var snapshot = coordinator.Build(new TraceFlowChannelContext(doc, ImpactedTestsBadge: 1, LastTestSummary: "1 failed"));

        Assert.Contains("n0->n1", snapshot.HighlightedEdgeKeys); // from code-flow
        Assert.Contains("n1->n2", snapshot.HighlightedEdgeKeys); // from unit-test
        Assert.Contains("n2", snapshot.HighlightedNodeIds);      // exit emphasis
    }

    private static SemanticMapSubgraphNode Node(string id, string kind) => new()
    {
        Id = id,
        Path = @"D:\w\A.cs",
        Kind = kind,
        Label = id
    };

    private static SemanticMapSubgraphEdge Edge(string from, string to, string kind) => new()
    {
        FromId = from,
        ToId = to,
        Kind = kind,
        RelatedKind = kind
    };
}
