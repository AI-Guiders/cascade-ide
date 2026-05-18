using Avalonia.Input;

namespace CascadeIDE.Features.Chat;

/// <summary>Сопоставление нажатия с настройкой «отправить сообщение» (Enter / Ctrl+Enter / Shift+Enter).</summary>
internal static class ChatSendKeyMatcher
{
    public static bool Matches(KeyEventArgs e, string sendMessageKey)
    {
        var isEnter = e.Key == Key.Enter;
        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        return sendMessageKey switch
        {
            "Enter" => isEnter && !ctrl && !shift,
            "Ctrl+Enter" => isEnter && ctrl && !shift,
            "Shift+Enter" => isEnter && !ctrl && shift,
            _ => false,
        };
    }
}
