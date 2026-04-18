using Avalonia;
using CascadeIDE.Cockpit.Cds;
using CascadeIDE.Cockpit.Channels.TraceFlow;
using CascadeIDE.Cockpit.Composition.TraceFlow;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class TraceFlowSurfaceCompositorTests
{
    [Fact]
    public void Compose_WhenEnabled_AppliesHighlights()
    {
        var scene = BuildScene();
        var snapshot = new TraceFlowChannelSnapshot(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "n0", "n1" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "n0->n1" });
        var compositor = new TraceFlowSurfaceCompositor();

        var result = compositor.Compose(scene, snapshot, new TraceFlowCdsDecision(true, "pfd", "normal"));

        Assert.Contains("n1", result.HighlightedNodeIds);
        Assert.Contains("n0->n1", result.HighlightedEdgeKeys);
    }

    [Fact]
    public void Compose_WhenDisabled_LeavesSceneUnhighlighted()
    {
        var scene = BuildScene();
        var snapshot = new TraceFlowChannelSnapshot(
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "n0", "n1" },
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "n0->n1" });
        var compositor = new TraceFlowSurfaceCompositor();

        var result = compositor.Compose(scene, snapshot, new TraceFlowCdsDecision(false, "none", "off"));

        Assert.Empty(result.HighlightedNodeIds);
        Assert.Empty(result.HighlightedEdgeKeys);
    }

    [Fact]
    public void Router_EnablesOnlyForControlFlowInPfd()
    {
        var cds = new CockpitSurfaceState(
            SchemaVersion: "0.3",
            UiMode: "flight",
            PresentationEffectiveLine: "line",
            PresentationParseSuccess: true,
            Topology: new CockpitSurfaceTopology("main", false, false, true),
            SecondaryShell: new CockpitSurfaceSecondaryShell("none"),
            Zones: new CockpitSurfaceZones(true, true, true, true, true, true),
            Instruments: []);

        var router = new TraceFlowCdsRouter();
        var enabled = router.Route(new TraceFlowCdsRouteInput(cds, "controlFlow"));
        var disabled = router.Route(new TraceFlowCdsRouteInput(cds, "file"));

        Assert.True(enabled.Enabled);
        Assert.False(disabled.Enabled);
    }

    private static SemanticMapGraphSceneVm BuildScene() => new()
    {
        Nodes =
        [
            Node("n0", true),
            Node("n1")
        ],
        Edges =
        [
            Edge("n0", "n1", "Call")
        ]
    };

    private static SemanticMapGraphNodeLayout Node(string id, bool anchor = false) => new()
    {
        Id = id,
        Kind = anchor ? "anchor" : "call_step",
        FullPath = @"D:\w\A.cs",
        Label = id,
        Center = new Point(0, 0),
        Radius = 10,
        IsAnchor = anchor
    };

    private static SemanticMapGraphEdgeLayout Edge(string from, string to, string kind) => new()
    {
        FromNodeId = from,
        ToNodeId = to,
        From = new Point(0, 0),
        To = new Point(1, 1),
        ToRadius = 10,
        Kind = kind,
        RelatedKind = kind
    };
}
