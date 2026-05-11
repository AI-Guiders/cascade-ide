using Avalonia.Input;
using CascadeIDE.Features.Shell.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CascadeChordMelodyKeyMapTests
{
    [Theory]
    [InlineData(Key.Space, KeyModifiers.None, PhysicalKey.Space, ' ')]
    [InlineData(Key.Space, KeyModifiers.Shift, PhysicalKey.Space, ' ')]
    public void Maps_space(Key key, KeyModifiers mods, PhysicalKey physical, char expected)
    {
        Assert.True(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(key, mods, physical, out var ch));
        Assert.Equal(expected, ch);
    }

    [Fact]
    public void Maps_semicolon_us_no_shift_physical_semicolon()
    {
        Assert.True(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(
            Key.OemSemicolon,
            KeyModifiers.None,
            PhysicalKey.Semicolon,
            out var ch));
        Assert.Equal(';', ch);
    }

    [Fact]
    public void Maps_semicolon_windows_oem1_no_shift()
    {
        Assert.True(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(
            Key.Oem1,
            KeyModifiers.None,
            PhysicalKey.None,
            out var ch));
        Assert.Equal(';', ch);
    }

    [Fact]
    public void Maps_colon_us_shift_semicolon_virtual_key()
    {
        Assert.True(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(
            Key.OemSemicolon,
            KeyModifiers.Shift,
            PhysicalKey.Semicolon,
            out var ch));
        Assert.Equal(':', ch);
    }

    [Fact]
    public void Maps_colon_by_physical_semicolon_when_layout_hides_oem_semicolon()
    {
        // Не-US: Key может быть не OemSemicolon, позиция ; на QWERTY всё та же.
        Assert.True(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(
            Key.Oem1,
            KeyModifiers.Shift,
            PhysicalKey.Semicolon,
            out var ch));
        Assert.Equal(':', ch);
    }

    [Fact]
    public void Maps_colon_ru_shift_digit6_physical()
    {
        Assert.True(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(
            Key.D6,
            KeyModifiers.Shift,
            PhysicalKey.Digit6,
            out var ch));
        Assert.Equal(':', ch);
    }

    [Fact]
    public void Does_not_map_when_ctrl_held()
    {
        Assert.False(CascadeChordMelodyKeyMap.TryMapChordMelodyGlyph(
            Key.Space,
            KeyModifiers.Control,
            PhysicalKey.Space,
            out _));
    }
}
