using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Services;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MonacoEditorM9MapperTests
{
    [Fact]
    public void InlayMapper_converts_offset_to_line_column()
    {
        const string text = "var x = 1;\nvar y = 2;";
        var parts = new[] { new EditorTrailingInlayPart(3, "  int") };
        var hints = MonacoEditorInlayMapper.ToHints(text, parts);
        Assert.Single(hints);
        Assert.Equal(1, hints[0].Line);
        Assert.Equal(4, hints[0].Column);
    }

    [Fact]
    public void CodeLensComposer_emits_nodes_with_line_start()
    {
        const string path = @"D:\w\A.cs";
        var scene = new CodeNavigationMapGraphSceneVm
        {
            Presentation = CodeNavigationMapGraphPresentationKind.CodeControlFlow,
            Nodes =
            [
                new CodeNavigationMapGraphNodeLayout
                {
                    Id = "n1",
                    FullPath = path,
                    Kind = "step",
                    Label = "DoWork",
                    IsAnchor = false,
                    Center = new Avalonia.Point(10, 10),
                    Radius = 6,
                    LineStart = 42,
                    LegendIndex = 3,
                },
            ],
            Edges = [],
        };

        var lenses = MonacoEditorCodeLensComposer.FromNavigationScene(path, scene);
        Assert.Single(lenses);
        Assert.Equal("n1", lenses[0].Id);
        Assert.Equal(42, lenses[0].Line);
        Assert.Contains("DoWork", lenses[0].Title);
    }
}
