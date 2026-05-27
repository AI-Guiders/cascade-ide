using Avalonia.Media;
using CascadeIDE.Cockpit.PrimitivesKit;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ControlFlowEditorNodePainterTests
{
    [Fact]
    public void ResolveFillBrush_matches_cfg_palette_for_call_and_handler()
    {
        var call = (SolidColorBrush)ControlFlowEditorNodePainter.ResolveFillBrush("call_step", ControlFlowNodeVisualKind.Circle);
        var handler = (SolidColorBrush)ControlFlowEditorNodePainter.ResolveFillBrush("handler_step", ControlFlowNodeVisualKind.Circle);
        var condition = (SolidColorBrush)ControlFlowEditorNodePainter.ResolveFillBrush(
            "condition_step",
            ControlFlowNodeVisualKind.Diamond);

        Assert.Equal(CockpitPrimitivesPalette.CodeNavigationMap.CallFill, call.Color);
        Assert.Equal(CockpitPrimitivesPalette.CodeNavigationMap.HandlerFill, handler.Color);
        Assert.Equal(CockpitPrimitivesPalette.CodeNavigationMap.ConditionFill, condition.Color);
    }

    [Fact]
    public void BuildGutterLineVisuals_includes_node_kind_for_painter()
    {
        var scene = new ViewModels.CodeNavigationMapGraphSceneVm
        {
            Presentation = ViewModels.CodeNavigationMapGraphPresentationKind.CodeControlFlow,
            Nodes =
            [
                new ViewModels.CodeNavigationMapGraphNodeLayout
                {
                    Id = "h",
                    Kind = "handler_step",
                    FullPath = @"D:\w\A.cs",
                    Label = "catch",
                    Center = default,
                    Radius = 8,
                    IsAnchor = false,
                    LineStart = 12
                }
            ],
            Edges = []
        };

        var visuals = CodeNavigationControlFlowGlyphComposer.BuildGutterLineVisuals(scene);
        Assert.Single(visuals);
        Assert.Equal("handler_step", visuals[0].NodeKind);
        Assert.Equal(ControlFlowNodeVisualKind.Circle, visuals[0].VisualKind);
    }
}
