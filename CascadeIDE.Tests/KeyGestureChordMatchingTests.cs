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
}
