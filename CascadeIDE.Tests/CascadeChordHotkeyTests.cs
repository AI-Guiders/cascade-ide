using Avalonia.Input;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CascadeChordHotkeyTests
{
    private static void AssertGestureEqual(KeyGesture expected, KeyGesture actual)
    {
        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.KeyModifiers, actual.KeyModifiers);
    }

    [Fact]
    public void ResolveRootGesture_MissingCascadeChord_UsesDefaultCtrlK()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["toggle_command_palette"] = "Ctrl+Q"
        };
        var g = CascadeChordHotkey.ResolveRootGesture(map);
        AssertGestureEqual(KeyGesture.Parse(CascadeChordHotkey.DefaultGestureString), g);
    }

    [Fact]
    public void ResolveRootGesture_EmptyCascadeChord_UsesDefaultCtrlK()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CascadeChordHotkey.TomlKey] = "   "
        };
        var g = CascadeChordHotkey.ResolveRootGesture(map);
        AssertGestureEqual(KeyGesture.Parse(CascadeChordHotkey.DefaultGestureString), g);
    }

    [Fact]
    public void ResolveRootGesture_ValidCustom_Parses()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CascadeChordHotkey.TomlKey] = "Ctrl+Shift+K"
        };
        var g = CascadeChordHotkey.ResolveRootGesture(map);
        AssertGestureEqual(KeyGesture.Parse("Ctrl+Shift+K"), g);
    }

    [Fact]
    public void ResolveRootGesture_InvalidString_FallsBackToCtrlK()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CascadeChordHotkey.TomlKey] = "%%%not_a_valid_gesture%%%"
        };
        var g = CascadeChordHotkey.ResolveRootGesture(map);
        AssertGestureEqual(KeyGesture.Parse(CascadeChordHotkey.DefaultGestureString), g);
    }

    [Fact]
    public void MatchesPhysicalKeyFallback_CtrlK_RequiresExactModifiers()
    {
        var resolved = KeyGesture.Parse("Ctrl+K");
        Assert.True(CascadeChordHotkey.MatchesPhysicalKeyFallback(resolved, PhysicalKey.K, KeyModifiers.Control));
        Assert.False(CascadeChordHotkey.MatchesPhysicalKeyFallback(resolved, PhysicalKey.K, KeyModifiers.Control | KeyModifiers.Shift));
        Assert.False(CascadeChordHotkey.MatchesPhysicalKeyFallback(resolved, PhysicalKey.J, KeyModifiers.Control));
    }
}
