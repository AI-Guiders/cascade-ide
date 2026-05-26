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
    public void Matches_respects_send_message_key_setting(string setting, Key key, KeyModifiers mods, bool expected)
    {
        var e = new KeyEventArgs { Key = key, KeyModifiers = mods };
        Assert.Equal(expected, ChatSendKeyMatcher.Matches(e, setting));
    }

    [Fact]
    public void Matches_CtrlPlusEnter_also_accepts_physical_return_key()
    {
        var e = new KeyEventArgs { Key = Key.Return, KeyModifiers = KeyModifiers.Control };
        Assert.True(ChatSendKeyMatcher.Matches(e, "Ctrl+Enter"));
    }

    [Fact]
    public void IsBareEnterForSlashCommit_true_only_for_plain_enter()
    {
        Assert.True(ChatSendKeyMatcher.IsBareEnterForSlashCommit(new KeyEventArgs { Key = Key.Enter, KeyModifiers = KeyModifiers.None }));
        Assert.True(ChatSendKeyMatcher.IsBareEnterForSlashCommit(new KeyEventArgs { Key = Key.Return, KeyModifiers = KeyModifiers.None }));
        Assert.False(ChatSendKeyMatcher.IsBareEnterForSlashCommit(new KeyEventArgs { Key = Key.Enter, KeyModifiers = KeyModifiers.Control }));
        Assert.False(ChatSendKeyMatcher.IsBareEnterForSlashCommit(new KeyEventArgs { Key = Key.Enter, KeyModifiers = KeyModifiers.Shift }));
    }
}
