using System.Collections.Immutable;

namespace CascadeIDE.Services;

/// <summary>Палитра: keyboard-first команды чата (выбор, thinking).</summary>
public static partial class IdeCommandRegistry
{
    private static void RegisterChatPalette(ImmutableArray<IdeCommandRegistryEntry>.Builder b)
    {
        AddPalette(b, "chat_select_prev_message", IdeCommands.ChatSelectPrevMessage, "Чат: выбрать предыдущее сообщение", "Чат");
        AddPalette(b, "chat_select_next_message", IdeCommands.ChatSelectNextMessage, "Чат: выбрать следующее сообщение", "Чат");
        AddPalette(b, "chat_select_prev_thread", IdeCommands.ChatSelectPrevThread, "Чат: выбрать предыдущую тему", "Чат");
        AddPalette(b, "chat_select_next_thread", IdeCommands.ChatSelectNextThread, "Чат: выбрать следующую тему", "Чат");
        AddPalette(b, "chat_open_selected_thread", IdeCommands.ChatOpenSelectedThread, "Чат: открыть detail выбранной темы", "Чат");
        AddPalette(b, "chat_show_thread_overview", IdeCommands.ChatShowThreadOverview, "Чат: вернуться к overview тем", "Чат");
        AddPalette(b, "chat_toggle_selected_thinking", IdeCommands.ChatToggleSelectedThinking, "Чат: свернуть/развернуть selected thinking", "Чат");
        AddPalette(
            b,
            "chat_toggle_show_thinking_in_history",
            IdeCommands.ChatToggleShowThinkingInHistory,
            "Чат: переключить show_thinking_in_history",
            "Чат");
    }
}
