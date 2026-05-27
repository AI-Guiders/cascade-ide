using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CodeNavigationControlFlowGlyphComposerTests
{
    private static CodeNavigationMapGraphSceneVm Scene(bool useLegendColumn, bool showGlyphs) =>
        new()
        {
            Nodes = [],
            Edges = [],
            UseLegendColumn = useLegendColumn,
            ShowNodeLegendGlyphs = showGlyphs
        };

    private static CodeNavigationMapGraphNodeLayout Node(
        string id,
        string kind,
        int line,
        bool anchor = false,
        int? legend = null,
        CodeNavigationMapNodeShape shape = CodeNavigationMapNodeShape.Circle,
        string label = "x") =>
        new()
        {
            Id = id,
            Kind = kind,
            FullPath = @"C:\x\A.cs",
            Label = label,
            Center = default,
            Radius = 9,
            IsAnchor = anchor,
            LineStart = line,
            Shape = shape,
            LegendIndex = legend
        };

    [Fact]
    public void Exit_yields_empty_glyph_and_arrow_flag()
    {
        var n = Node("e", "exit_step", 99);
        var (g, a) = CodeNavigationControlFlowGlyphComposer.BuildTextGlyph(n, Scene(false, false));
        Assert.Equal("", g);
        Assert.True(a);
        Assert.Equal(ControlFlowNodeVisualKind.Exit, CodeNavigationControlFlowGlyphComposer.ResolveVisualKind(n));
    }

    [Fact]
    public void Protected_step_shows_T()
    {
        var n = Node("p", "protected_step", 2);
        var (g, _) = CodeNavigationControlFlowGlyphComposer.BuildTextGlyph(n, Scene(false, false));
        Assert.Equal("T", g);
    }

    [Fact]
    public void Handler_shows_bang()
    {
        var n = Node("h", "handler_step", 6);
        var (g, _) = CodeNavigationControlFlowGlyphComposer.BuildTextGlyph(n, Scene(false, false));
        Assert.Equal("!", g);
    }

    [Fact]
    public void Legend_number_when_column_or_node_glyphs()
    {
        var n = Node("s", "call_step", 3, legend: 4);
        var (g1, _) = CodeNavigationControlFlowGlyphComposer.BuildTextGlyph(n, Scene(useLegendColumn: true, showGlyphs: false));
        Assert.Equal("4", g1);
        var (g2, _) = CodeNavigationControlFlowGlyphComposer.BuildTextGlyph(n, Scene(useLegendColumn: false, showGlyphs: true));
        Assert.Equal("4", g2);
    }

    [Fact]
    public void Condition_shows_question_and_diamond()
    {
        var n = Node("c", "condition_step", 5);
        var (g, _) = CodeNavigationControlFlowGlyphComposer.BuildTextGlyph(n, Scene(false, false));
        Assert.Equal("?", g);
        Assert.Equal(ControlFlowNodeVisualKind.Diamond, CodeNavigationControlFlowGlyphComposer.ResolveVisualKind(n));
    }
}
