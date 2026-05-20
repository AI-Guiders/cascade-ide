using Avalonia.Input;
using CascadeIDE.Features.Chat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class ChatSendKeyMatcherTests
{
    [Theory]
    [InlineData("Enter", Key.Enter, KeyModifiers.None, true)]
    [InlineData("Enter", Key.Enter, KeyModifiers.Control, false)]
    [InlineData("Ctrl+Enter", Key.Enter, KeyModifiers.Control, true)]
    [InlineData("Shift+Enter", Key.Enter, KeyModifiers.Shift, true)]
    [InlineData("Ctrl+Enter", Key.Return, KeyModifiers.Control, true)]
    public void Matches_respects_send_message_key_setting(string setting, Key key, KeyModifiers mods, bool expected)
    {
        var e = new KeyEventArgs { Key = key, KeyModifiers = mods };
        Assert.Equal(expected, ChatSendKeyMatcher.Matches(e, setting));
    }
}
