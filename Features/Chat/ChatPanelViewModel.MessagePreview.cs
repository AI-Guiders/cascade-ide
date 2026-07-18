#nullable enable

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    public Action<string, string>? ShowMarkdownPreview { get; set; }

    public bool TryGetMessageContent(int messageIndex, out string content)
    {
        content = "";
        if (messageIndex < 0 || messageIndex >= ChatMessages.Count)
            return false;

        content = ChatMessages[messageIndex].Content ?? "";
        return !string.IsNullOrWhiteSpace(content);
    }

    public void OpenMessageMarkdownPreview(int messageIndex)
    {
        if (!TryGetMessageContent(messageIndex, out var body))
            return;

        var role = messageIndex >= 0 && messageIndex < ChatMessages.Count
            ? ChatMessages[messageIndex].Role
            : "message";
        var title = role switch
        {
            "user" => "Сообщение · Ты",
            "assistant" => "Сообщение · Агент",
            _ => $"Сообщение · {role}",
        };
        ShowMarkdownPreview?.Invoke(title, body);
    }
}
