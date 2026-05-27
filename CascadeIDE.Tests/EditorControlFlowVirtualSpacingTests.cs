using Avalonia;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Models;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorControlFlowVirtualSpacingTests
{
    [Fact]
    public void ShouldReserveLane_false_when_not_control_flow_level()
    {
        var scene = MinimalCfScene();
        Assert.False(EditorControlFlowVirtualSpacing.ShouldReserveLane(
            CodeNavigationMapLevelKind.File,
            @"D:\w\A.cs",
            @"D:\w\A.cs",
            scene));
    }

    [Fact]
    public void ShouldReserveLane_false_when_anchor_mismatch()
    {
        var scene = MinimalCfScene();
        Assert.False(EditorControlFlowVirtualSpacing.ShouldReserveLane(
            CodeNavigationMapLevelKind.ControlFlow,
            @"D:\w\A.cs",
            @"D:\w\B.cs",
            scene));
    }

    [Fact]
    public void ShouldReserveLane_true_when_cf_scene_for_same_file()
    {
        const string path = @"D:\w\A.cs";
        var scene = MinimalCfScene();
        Assert.True(EditorControlFlowVirtualSpacing.ShouldReserveLane(
            CodeNavigationMapLevelKind.ControlFlow,
            path,
            path,
            scene));
    }

    [Fact]
    public void LaneWidthPixels_fits_glyph_diameter_plus_padding()
    {
        double expected = EditorControlFlowVirtualSpacing.GlyphRadius * 2
            + EditorControlFlowVirtualSpacing.LanePadding * 2;
        Assert.Equal(EditorControlFlowVirtualSpacing.LaneWidthPixels, expected);
    }

    private static CodeNavigationMapGraphSceneVm MinimalCfScene() =>
        new()
        {
            Presentation = CodeNavigationMapGraphPresentationKind.CodeControlFlow,
            Nodes =
            [
                new CodeNavigationMapGraphNodeLayout
                {
                    Id = "n0",
                    FullPath = @"D:\w\A.cs",
                    Kind = "anchor",
                    Label = "A.cs",
                    IsAnchor = true,
                    Center = new Point(40, 40),
                    Radius = 8
                }
            ],
            Edges = [],
            ControlFlowMainAxis = GraphControlFlowMainAxis.Vertical
        };
}
