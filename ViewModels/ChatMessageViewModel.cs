using CascadeIDE.Features.Chat;
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

    public string? SlashCommandPath { get; private set; }

    public string? SlashCommandArgs { get; private set; }

    [ObservableProperty]
    private ChatSlashCommandStatus? _slashCommandStatus;

    public bool IsSlashCommand => SlashCommandStatus is not null;

    public ChatMessageViewModel(
        string role,
        string content,
        Guid? messageId = null,
        Guid? threadId = null,
        Guid? parentMessageId = null,
        string? slashCommandPath = null,
        string? slashCommandArgs = null,
        ChatSlashCommandStatus? slashCommandStatus = null)
    {
        MessageId = messageId ?? Guid.NewGuid();
        Role = role;
        ThreadId = threadId ?? Guid.Empty;
        ParentMessageId = parentMessageId;
        _content = content;
        SlashCommandPath = slashCommandPath;
        SlashCommandArgs = slashCommandArgs;
        _slashCommandStatus = slashCommandStatus;
    }

    public static ChatMessageViewModel CreateSlashCommand(
        string slashPath,
        string? args,
        Guid? threadId = null) =>
        new(
            "slash_command",
            "",
            threadId: threadId,
            slashCommandPath: slashPath,
            slashCommandArgs: args,
            slashCommandStatus: ChatSlashCommandStatus.Running);

    public void ApplySlashCommandResult(in ChatSlashCommandRunResult result)
    {
        SlashCommandPath = result.SlashPath;
        SlashCommandArgs = ChatSlashCommandPresentation.NormalizeArgsTail(result.ArgsTail);
        SlashCommandStatus = result.Success
            ? ChatSlashCommandStatus.Succeeded
            : ChatSlashCommandStatus.Failed;
        Content = result.DetailText ?? "";
        OnPropertyChanged(nameof(SlashCommandPath));
        OnPropertyChanged(nameof(SlashCommandArgs));
        OnPropertyChanged(nameof(IsSlashCommand));
    }
}
