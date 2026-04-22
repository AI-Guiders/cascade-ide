using CascadeIDE.Services.ChordNotation;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChordNotationParserTests
{
    [Fact]
    public void Empty_string_yields_success_zero_steps()
    {
        var r = ChordNotationParser.Parse("");
        Assert.True(r.IsSuccess);
        Assert.Empty(r.Steps);
    }

    [Fact]
    public void CascadeChord_ctrl_k_palette_steps_for_map_and_mfd()
    {
        var r = ChordNotationParser.Parse("<C-k> s p");
        Assert.True(r.IsSuccess);
        Assert.Collection(
            r.Steps,
            s => AssertChord(s, ["C-"], "k"),
            s => AssertPlain(s, "s"),
            s => AssertPlain(s, "p"));

        var z = ChordNotationParser.Parse("<C-k> m m");
        Assert.True(z.IsSuccess);
        Assert.Collection(
            z.Steps,
            s => AssertChord(s, ["C-"], "k"),
            s => AssertPlain(s, "m"),
            s => AssertPlain(s, "m"));
    }

    [Fact]
    public void Bracket_multi_modifiers_and_plain_Esc()
    {
        var r = ChordNotationParser.Parse("<C-M-n>");
        Assert.True(r.IsSuccess);
        Assert.Single(r.Steps);
        AssertChord(r.Steps[0], ["C-", "M-"], "n");

        var e = ChordNotationParser.Parse("Esc");
        Assert.True(e.IsSuccess);
        AssertPlain(e.Steps[0], "Esc");
    }

    [Fact]
    public void FMS_style_plain_tokens()
    {
        var r = ChordNotationParser.Parse("L1 R2 EXEC");
        Assert.True(r.IsSuccess);
        Assert.Collection(
            r.Steps,
            s => AssertPlain(s, "L1"),
            s => AssertPlain(s, "R2"),
            s => AssertPlain(s, "EXEC"));
    }

    [Fact]
    public void Unclosed_bracket_fails()
    {
        var r = ChordNotationParser.Parse("<C-k");
        Assert.False(r.IsSuccess);
        Assert.NotEmpty(r.Error);
    }

    private static void AssertPlain(ChordNotationStep step, string token)
    {
        var p = Assert.IsType<ChordNotationPlainStep>(step);
        Assert.Equal(token, p.Token);
    }

    private static void AssertChord(ChordNotationStep step, string[] mods, string key)
    {
        var c = Assert.IsType<ChordNotationChordStep>(step);
        Assert.Equal(mods, c.ModifierPrefixes);
        Assert.Equal(key, c.Key);
    }
}
