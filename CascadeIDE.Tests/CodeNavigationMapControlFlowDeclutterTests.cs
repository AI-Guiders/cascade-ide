#nullable enable
using CascadeIDE.Cockpit.Graph;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services.SkiaInstruments;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationMapControlFlowDeclutterTests
{
    [Fact]
    public void TryTransform_Normal_CollapsesWhenFanOutAtLeastFour()
    {
        var doc = new GraphDocument
        {
            AnchorPath = @"D:\w\A.cs",
            Nodes =
            [
                new GraphNode { Id = "h", Path = @"D:\w\A.cs", Kind = "condition_step", Label = "?" },
                new GraphNode { Id = "a", Path = @"D:\w\A.cs", Kind = "call_step", Label = "1" },
                new GraphNode { Id = "b", Path = @"D:\w\A.cs", Kind = "call_step", Label = "2" },
                new GraphNode { Id = "c", Path = @"D:\w\A.cs", Kind = "call_step", Label = "3" },
                new GraphNode { Id = "d", Path = @"D:\w\A.cs", Kind = "call_step", Label = "4" },
                new GraphNode { Id = "e", Path = @"D:\w\A.cs", Kind = "call_step", Label = "5" },
            ],
            Edges =
            [
                new GraphEdge { FromId = "h", ToId = "a", Kind = "MultiBranch" },
                new GraphEdge { FromId = "h", ToId = "b", Kind = "MultiBranch" },
                new GraphEdge { FromId = "h", ToId = "c", Kind = "MultiBranch" },
                new GraphEdge { FromId = "h", ToId = "d", Kind = "MultiBranch" },
                new GraphEdge { FromId = "h", ToId = "e", Kind = "MultiBranch" },
            ]
        };

        var state = new CodeNavigationMapPipelineState(
            doc,
            CodeNavigationMapLevelKind.ControlFlow,
            new SkiaInstrumentViewport(300, 140),
            CodeNavigationMapDetailLevel.Normal,
            CodeNavigationMapRelatedGraphLayoutKind.Radial,
            CodeNavigationMapControlFlowMainAxisKind.Auto,
            null,
            3,
            0,
            5,
            false);

        var result = CodeNavigationMapControlFlowDeclutter.TryTransform(state);
        Assert.NotNull(result);
        Assert.Contains(result.Nodes, n => n.Label == "+2");
        Assert.Equal(4, result.Edges.Count(e =>
            e.Kind?.Contains("multibranch", StringComparison.OrdinalIgnoreCase) == true));
    }
}
