#nullable enable
using Avalonia;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapGraphSceneProjectionTests
{
    [Fact]
    public void RoundTrip_PreservesLoopGroupIdAndControlFlowMainAxis()
    {
        var layout = new GraphLayoutScene
        {
            Nodes =
            [
                new GraphLayoutNode
                {
                    Id = "a",
                    Kind = "anchor",
                    FullPath = "p",
                    Label = "A",
                    Center = new Point(1, 2),
                    Radius = 10,
                    IsAnchor = true,
                    LoopGroupId = null,
                },
                new GraphLayoutNode
                {
                    Id = "b",
                    Kind = "loop_step",
                    FullPath = "p",
                    Label = "for",
                    Center = new Point(3, 4),
                    Radius = 9,
                    IsAnchor = false,
                    LoopGroupId = 7,
                },
            ],
            Edges = [],
            ControlFlowMainAxis = GraphControlFlowMainAxis.Horizontal,
        };

        var vm = CodeNavigationMapGraphSceneProjection.ToViewModel(layout, 200, 120);
        Assert.Equal(7, vm.Nodes[1].LoopGroupId);
        Assert.Equal(GraphControlFlowMainAxis.Horizontal, vm.ControlFlowMainAxis);

        var back = CodeNavigationMapGraphSceneProjection.FromViewModel(vm);
        Assert.Equal(7, back.Nodes[1].LoopGroupId);
        Assert.Equal(GraphControlFlowMainAxis.Horizontal, back.ControlFlowMainAxis);
    }
}
