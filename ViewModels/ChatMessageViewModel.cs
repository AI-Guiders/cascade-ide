using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public sealed partial class ChatMessageViewModel : ObservableObject
{
    /// <summary>Стабильный идентификатор для лога событий и MCP (редактирование, ссылки).</summary>
    public Guid MessageId { get; }

    public string Role { get; }

    [ObservableProperty]
    private string _content;

    public ChatMessageViewModel(string role, string content, Guid? messageId = null)
    {
        MessageId = messageId ?? Guid.NewGuid();
        Role = role;
        _content = content;
    }
}
