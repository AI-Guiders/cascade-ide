using CommunityToolkit.Mvvm.ComponentModel;

namespace CascadeIDE.ViewModels;

public sealed partial class ChatMessageViewModel : ObservableObject
{
    /// <summary>Стабильный идентификатор для лога событий и MCP (редактирование, ссылки).</summary>
    public Guid MessageId { get; }

    public string Role { get; }

    /// <summary>Ветка диалога; сообщения с разными thread_id — параллельные линии в одной сессии.</summary>
    public Guid ThreadId { get; }

    /// <summary>Родительское сообщение при ответной ветке; иначе null.</summary>
    public Guid? ParentMessageId { get; }

    [ObservableProperty]
    private string _content;

    public ChatMessageViewModel(
        string role,
        string content,
        Guid? messageId = null,
        Guid? threadId = null,
        Guid? parentMessageId = null)
    {
        MessageId = messageId ?? Guid.NewGuid();
        Role = role;
        ThreadId = threadId ?? Guid.Empty;
        ParentMessageId = parentMessageId;
        _content = content;
    }
}
