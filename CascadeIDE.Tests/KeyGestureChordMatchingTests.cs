using Avalonia.Input;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class KeyGestureChordMatchingTests
{
    [Fact]
    public void Matches_CtrlQ_WrongVirtualKey_UsesPhysicalQwertyPosition()
    {
        var gesture = KeyGesture.Parse("Ctrl+Q");
        var e = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.W,
            KeyModifiers = KeyModifiers.Control,
            PhysicalKey = PhysicalKey.Q
        };
        Assert.True(KeyGestureChordMatching.Matches(gesture, e));
    }

    [Fact]
    public void Matches_CtrlK_WrongVirtualKey_UsesPhysicalQwertyPosition()
    {
        var gesture = KeyGesture.Parse("Ctrl+K");
        var e = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.A,
            KeyModifiers = KeyModifiers.Control,
            PhysicalKey = PhysicalKey.K
        };
        Assert.True(KeyGestureChordMatching.Matches(gesture, e));
    }

    [Fact]
    public void Matches_StrictFirst_PassesThrough()
    {
        var gesture = KeyGesture.Parse("Ctrl+Q");
        var e = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Q,
            KeyModifiers = KeyModifiers.Control,
            PhysicalKey = PhysicalKey.Q
        };
        Assert.True(KeyGestureChordMatching.Matches(gesture, e));
    }

    /// <summary>Регрессия: Ctrl+Shift+буква в TOML — жест должен матчиться с реальным KeyDown (mods включают Shift).</summary>
    [Fact]
    public void Matches_CtrlShiftO_SyntheticEvent_Matches()
    {
        var gesture = KeyGesture.Parse("Ctrl+Shift+O");
        var e = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.O,
            KeyModifiers = KeyModifiers.Control | KeyModifiers.Shift,
            PhysicalKey = PhysicalKey.O
        };
        Assert.True(KeyGestureChordMatching.Matches(gesture, e));
    }

}
