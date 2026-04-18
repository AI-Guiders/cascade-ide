using CascadeIDE.Services.ChordNotation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChordNotationSemanticTests
{
    [Fact]
    public void Vim_and_KeyGesture_normalize_to_same_sequence_for_equivalent_chords()
    {
        Assert.True(ChordNotationParser.TryParseVimToNormalized("<C-k> s p", out var vim, out _));
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl+K s p", out var kg, out _));
        Assert.NotNull(vim);
        Assert.NotNull(kg);
        Assert.Equal(3, vim!.Steps.Count);
        Assert.Equal(vim.Steps.Count, kg!.Steps.Count);
        AssertNormalizedChord(vim.Steps[0], ChordModifierKeys.Control, "K");
        AssertNormalizedChord(kg.Steps[0], ChordModifierKeys.Control, "K");
        AssertNormalizedPlain(vim.Steps[1], "S");
        AssertNormalizedPlain(kg.Steps[1], "S");
        AssertNormalizedPlain(vim.Steps[2], "P");
        AssertNormalizedPlain(kg.Steps[2], "P");
    }

    [Fact]
    public void KeyGesture_Ctrl_Shift_P_and_unicode_command_key()
    {
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl + Shift + P", out var a, out _));
        Assert.NotNull(a);
        Assert.Single(a!.Steps);
        var ch = Assert.IsType<NormalizedChordStep>(a.Steps[0]);
        Assert.Equal(ChordModifierKeys.Control | ChordModifierKeys.Shift, ch.Modifiers);
        Assert.Equal("P", ch.KeySymbol);

        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("\u2318K", out var b, out _));
        var ch2 = Assert.IsType<NormalizedChordStep>(b!.Steps[0]);
        Assert.Equal(ChordModifierKeys.Meta, ch2.Modifiers);
        Assert.Equal("K", ch2.KeySymbol);
    }

    [Fact]
    public void KeyGesture_sequence_two_chords()
    {
        Assert.True(KeyGestureChordSyntax.TryParseToNormalized("Ctrl+K Ctrl+C", out var s, out _));
        Assert.NotNull(s);
        Assert.Equal(2, s!.Steps.Count);
        AssertNormalizedChord(s.Steps[0], ChordModifierKeys.Control, "K");
        AssertNormalizedChord(s.Steps[1], ChordModifierKeys.Control, "C");
    }

    private static void AssertNormalizedChord(NormalizedSequenceStep step, ChordModifierKeys mods, string key)
    {
        var c = Assert.IsType<NormalizedChordStep>(step);
        Assert.Equal(mods, c.Modifiers);
        Assert.Equal(key, c.KeySymbol);
    }

    private static void AssertNormalizedPlain(NormalizedSequenceStep step, string key)
    {
        var p = Assert.IsType<NormalizedPlainKeyStep>(step);
        Assert.Equal(key, p.KeySymbol);
    }
}
