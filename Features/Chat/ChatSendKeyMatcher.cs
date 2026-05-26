using Avalonia.Input;

namespace CascadeIDE.Features.Chat;

/// <summary>Сопоставление нажатия с настройкой «отправить сообщение» (Enter / Ctrl+Enter / Shift+Enter).</summary>
internal static class ChatSendKeyMatcher
{
    public static bool IsEnterPhysicalKey(Key key) => key is Key.Enter or Key.Return;

    /// <summary>
    /// Physical Enter без модификаторов. Для завершённого runnable slash (ADR 0150): коммитим на Enter,
    /// даже если глобальная «отправить» — Ctrl+Enter / Shift+Enter (иначе в composer вставляется newline).
    /// </summary>
    public static bool IsBareEnterForSlashCommit(KeyEventArgs e)
    {
        if (!IsEnterPhysicalKey(e.Key))
            return false;
        return (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Meta)) == 0;
    }

    public static bool Matches(KeyEventArgs e, string sendMessageKey)
    {
        var isEnter = IsEnterPhysicalKey(e.Key);
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
