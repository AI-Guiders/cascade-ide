using System.Collections.ObjectModel;
using Avalonia.Threading;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Chat;

/// <summary>
/// Правая панель: история чата, ввод и отправка к LLM. Контекст редактора и настройки провайдера приходят с <see cref="MainWindowViewModel"/> через замыкания.
/// </summary>
public partial class ChatPanelViewModel : ViewModelBase
{
    private readonly Services.AiProviderManager _aiProviderManager;
    private readonly Func<string> _getActiveAiProvider;
    private readonly Func<string?> _getSelectedOllamaModel;
    private readonly Func<bool> _getUseMinimizedContext;
    private readonly Func<string?> _getCurrentFilePath;
    private readonly Func<string> _getEditorText;

    public ChatPanelViewModel(
        Services.AiProviderManager aiProviderManager,
        Func<string> getActiveAiProvider,
        Func<string?> getSelectedOllamaModel,
        Func<bool> getUseMinimizedContext,
        Func<string?> getCurrentFilePath,
        Func<string> getEditorText)
    {
        _aiProviderManager = aiProviderManager;
        _getActiveAiProvider = getActiveAiProvider;
        _getSelectedOllamaModel = getSelectedOllamaModel;
        _getUseMinimizedContext = getUseMinimizedContext;
        _getCurrentFilePath = getCurrentFilePath;
        _getEditorText = getEditorText;
        ChatMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChatMessages));
    }

    /// <summary>Вызвать из главного окна при смене провайдера/модели, влияющих на <see cref="CanSendChat"/>.</summary>
    public void RefreshSendChatCommandState() => SendChatCommand.NotifyCanExecuteChanged();

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];

    public bool HasChatMessages => ChatMessages.Count > 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private string _chatInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private bool _isChatLoading;

    [RelayCommand(CanExecute = nameof(CanSendChat))]
    private async Task SendChatAsync()
    {
        var input = ChatInput.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        ChatInput = "";
        ChatMessages.Add(new ChatMessageViewModel("user", input));
        IsChatLoading = true;

        try
        {
            var messages = ChatMessages.Take(ChatMessages.Count - 1)
                .Select(m => new Services.ChatMessage(m.Role, m.Content))
                .Append(new Services.ChatMessage("user", input))
                .ToList();
            var assistantMsg = new ChatMessageViewModel("assistant", "");
            ChatMessages.Add(assistantMsg);

            await foreach (var token in _aiProviderManager.StreamChatAsync(
                _getActiveAiProvider(),
                messages,
                _getCurrentFilePath(),
                _getEditorText(),
                _getUseMinimizedContext(),
                CancellationToken.None))
            {
                var t = token;
                Dispatcher.UIThread.Post(() => assistantMsg.Content += t);
            }
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsChatLoading = false);
        }
    }

    private bool CanSendChat() => !string.IsNullOrWhiteSpace(ChatInput)
        && !IsChatLoading
        && (_getActiveAiProvider() != "Ollama"
            || (!string.IsNullOrEmpty(_getSelectedOllamaModel())
                && _getSelectedOllamaModel() != MainWindowViewModel.InstallNewSentinel));
}
