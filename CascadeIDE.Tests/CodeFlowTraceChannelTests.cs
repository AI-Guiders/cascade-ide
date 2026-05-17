using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeFlowTraceChannelTests
{
    [Fact]
    public void Build_PrefersNonExitBranch()
    {
        var doc = new GraphDocument
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

        var channel = new CodeFlowTraceChannel();
        var snapshot = channel.Build(new TraceFlowChannelContext(doc, 0, ""));

        Assert.Contains("n0", snapshot.HighlightedNodeIds);
        Assert.Contains("n1", snapshot.HighlightedNodeIds);
        Assert.Contains("n3", snapshot.HighlightedNodeIds);
        Assert.Contains("n0->n1", snapshot.HighlightedEdgeKeys);
        Assert.Contains("n1->n3", snapshot.HighlightedEdgeKeys);
        Assert.DoesNotContain("n1->n2", snapshot.HighlightedEdgeKeys);
    }

    private static GraphNode Node(string id, string kind) => new()
    {
        Id = id,
        Path = @"D:\w\A.cs",
        Kind = kind,
        Label = id
    };

    private static GraphEdge Edge(string from, string to, string kind) => new()
    {
        FromId = from,
        ToId = to,
        Kind = kind,
        RelationKind = kind
    };
}
