#nullable enable
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Chat;

/// <summary>Thinking/tool bubble helpers shared by Cursor ACP (and related) send paths.</summary>
public partial class ChatPanelViewModel
{
    private ChatMessageViewModel CreateThoughtMessage()
    {
        var vm = new ChatMessageViewModel("thinking", "", threadId: _activeThreadId);
        ChatMessages.Add(vm);
        return vm;
    }

    private ChatMessageViewModel CreateToolMessage()
    {
        var vm = new ChatMessageViewModel("tool", "Вызов инструментов ACP…", threadId: _activeThreadId);
        ChatMessages.Add(vm);
        return vm;
    }

    private void FinalizeThinkingMessage(ChatMessageViewModel? thoughtMsg)
    {
        if (thoughtMsg is null)
            return;
        if (!_getShowThinkingInHistory())
        {
            ChatMessages.Remove(thoughtMsg);
            return;
        }

        var full = thoughtMsg.Content;
        if (string.IsNullOrWhiteSpace(full))
            return;
        var normalized = full.Trim();
        _collapsedThinkingByMessageId[thoughtMsg.MessageId] = normalized;
        thoughtMsg.Content = BuildCollapsedThinkingPreview(normalized);
    }

    private static void FinalizeToolMessage(ChatMessageViewModel? toolMsg, bool isError)
    {
        if (toolMsg is null)
            return;
        toolMsg.Content = isError
            ? "Инструменты ACP завершились с ошибкой."
            : "Инструменты ACP выполнены.";
    }

    private static string BuildCollapsedThinkingPreview(string fullThinking)
    {
        var preview = fullThinking.Length <= 180 ? fullThinking : fullThinking[..180].TrimEnd() + "…";
        return CollapsedThinkingPrefix + preview;
    }
}
