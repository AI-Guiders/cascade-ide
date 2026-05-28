using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Cockpit.Graph.Layout;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class GraphRelatedFileHierarchyLayoutEngineTests
{
    /// <summary>
    /// Регрессия 2026-05: <c>Math.Clamp(step, 38, innerH/(n+1))</c> при <c>38 &gt; innerH/(n+1)</c>
    /// (типично ~32 при умеренной высоте) кидал ArgumentException → «Не удалось разобрать ответ навигации».
    /// </summary>
    [Theory]
    [InlineData(5, 200)]
    [InlineData(37, 520)]
    public void Layout_top_down_regression_inverted_math_clamp_bounds_does_not_throw(int satelliteCount, double height)
    {
        var doc = BuildStarDoc(satelliteCount);
        var engine = new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: true);

        var ex = Record.Exception(() =>
            engine.Layout(doc, width: 280, height, CodeNavigationMapDetailLevel.Normal));

        Assert.Null(ex);
    }

    [Fact]
    public void Layout_many_satellites_top_down_places_all_nodes()
    {
        var doc = BuildStarDoc(satelliteCount: 37);
        var engine = new GraphRelatedFileHierarchyLayoutEngine(anchorAtTop: true);

        var scene = engine.Layout(doc, width: 280, height: 520, CodeNavigationMapDetailLevel.Normal);

        Assert.Equal(38, scene.Nodes.Count);
        Assert.All(scene.Nodes, n => Assert.True(n.Radius > 0));
        Assert.All(scene.Nodes, n => Assert.Equal(GraphNodeShape.Rectangle, n.Shape));
    }

    [Fact]
    public void Compositor_file_top_down_many_related_nodes_does_not_throw()
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        for (var i = 1; i <= 37; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = $"n{i}",
                Path = $@"D:\w\B{i}.cs",
                Kind = "project_peer",
                Label = $"B{i}.cs"
            });
        }

        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Kind = GraphKind.RelatedFiles,
            Nodes = nodes,
            Edges = nodes.Skip(1).Select(n => new GraphEdge { FromId = "n0", ToId = n.Id, Kind = "related_to" }).ToList()
        };

        var compositor = new CodeNavigationMapCompositor();
        var result = compositor.Compose(
            new CodeNavigationMapCompositionIntent(
                doc,
                CodeNavigationMapLevelKind.File,
                CodeNavigationMapDetailLevel.Normal,
                CodeNavigationMapRelatedGraphLayoutKind.TopDown,
                CodeNavigationMapControlFlowMainAxisKind.Auto),
            new Services.SkiaInstruments.SkiaInstrumentViewport(280, 120));

        Assert.True(result.PreferredHeight >= 120);
        Assert.Equal(38, result.ToSceneVm(280, result.PreferredHeight).Nodes.Count);
    }

    private static GraphDocument BuildStarDoc(int satelliteCount)
    {
        var nodes = new List<GraphNode>
        {
            new() { Id = "n0", Path = @"D:\w\A.cs", Kind = "anchor", Label = "A.cs" }
        };
        for (var i = 1; i <= satelliteCount; i++)
        {
            nodes.Add(new GraphNode
            {
                Id = $"n{i}",
                Path = $@"D:\w\B{i}.cs",
                Kind = "project_peer",
                Label = $"B{i}.cs"
            });
        }

        return new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes = nodes,
            Edges = nodes.Skip(1).Select(n => new GraphEdge { FromId = "n0", ToId = n.Id }).ToList()
        };
    }
}
