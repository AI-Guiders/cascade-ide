using CascadeIDE.Services.ChordNotation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChordNotationRendererTests
{
    [Fact]
    public void Windows_sequence_matches_KeyGesture_style()
    {
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl+K s p", out var seq, out _));
        var text = ChordNotationRenderer.Windows.Render(seq);
        Assert.Equal("Ctrl+K S P", text);
    }

    [Fact]
    public void Mac_symbols_sequence_uses_glyphs()
    {
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl+Shift+P", out var seq, out _));
        var text = ChordNotationRenderer.MacSymbols.Render(seq);
        Assert.Equal("\u2303\u21E7P", text); // ⌃⇧P
    }

    [Fact]
    public void FormatChord_windows_Meta()
    {
        var t = ChordNotationRenderer.FormatChord(ChordModifierKeys.Meta | ChordModifierKeys.Control, "K", ChordNotationRenderFlavor.Windows);
        Assert.Equal("Ctrl+Win+K", t);
    }

    [Fact]
    public void FormatChord_mac_Meta_is_Command_glyph()
    {
        var t = ChordNotationRenderer.FormatChord(ChordModifierKeys.Meta, "K", ChordNotationRenderFlavor.MacSymbols);
        Assert.Equal("\u2318K", t); // ⌘K
    }
}
