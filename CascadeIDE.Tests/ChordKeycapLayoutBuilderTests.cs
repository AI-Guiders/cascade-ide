using CascadeIDE.Services.ChordNotation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChordKeycapLayoutBuilderTests
{
    [Fact]
    public void Ctrl_shift_p_three_modifier_caps_plus_key()
    {
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl+Shift+P", out var seq, out _));
        var layout = ChordKeycapLayoutBuilder.Build(seq, ChordKeycapLabelFlavor.WindowsWords);
        Assert.Single(layout.Steps);
        var segs = layout.Steps[0].Segments;
        Assert.Equal(3, segs.Count);
        Assert.Equal("Ctrl", segs[0].Label);
        Assert.Equal("Shift", segs[1].Label);
        Assert.Equal("P", segs[2].Label);
    }

    [Fact]
    public void Mac_glyphs_one_segment_per_modifier()
    {
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl+Shift+P", out var seq, out _));
        var layout = ChordKeycapLayoutBuilder.Build(seq, ChordKeycapLabelFlavor.MacGlyphs);
        var segs = layout.Steps[0].Segments;
        Assert.Equal(3, segs.Count);
        Assert.Equal("\u2303", segs[0].Label);
        Assert.Equal("\u21E7", segs[1].Label);
        Assert.Equal("P", segs[2].Label);
    }

    [Fact]
    public void Sequence_preserves_multiple_steps()
    {
        Assert.True(ChordNotationParser.TryParseVimToNormalized("<C-k> m", out var seq, out _));
        var layout = ChordNotationRenderer.BuildKeycapLayout(seq);
        Assert.Equal(2, layout.Steps.Count);
        Assert.Equal(2, layout.Steps[0].Segments.Count); // Ctrl + K
        Assert.Single(layout.Steps[1].Segments);
    }
}
