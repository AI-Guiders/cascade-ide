#nullable enable

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CascadeIDE.Features.Chat.Application;
using CascadeIDE.Models;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
using CascadeIDE.Services.CursorAcp;
using CascadeIDE.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CascadeIDE.Features.Chat;

public partial class ChatPanelViewModel
{
    /// <summary>Вызвать из главного окна при смене провайдера/модели, влияющих на <see cref="CanSendChat"/>.</summary>
    public void RefreshSendChatCommandState() => SendChatCommand.NotifyCanExecuteChanged();

    /// <summary>Клавиша отправки из настроек (Enter / Ctrl+Enter / Shift+Enter).</summary>
    public string GetSendMessageKey() => _getSendMessageKey();

    /// <summary>Сочетание для переноса строки в composer (отдельно от отправки).</summary>
    public string GetComposerNewLineKey() => _getComposerNewLineKey();

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];
    public ObservableCollection<ClarificationDraftItemViewModel> ClarificationDraftItems { get; } = [];
    public ObservableCollection<CursorAcpModelPick> CursorAcpModelPicks { get; } = [];

    public bool HasChatMessages => ChatMessages.Count > 0;
    public bool HasActiveClarificationBatch => _activeClarificationBatch is not null;

    /// <summary>AEE time accounting trace (ADR 0148 W3).</summary>
    public void AppendAgentEnvironmentTrace(string text, ChatSlashCommandStatus status)
    {
        var vm = new ChatMessageViewModel(
            "assistant",
            text.Trim(),
            threadId: ResolveMessageThreadId(),
            slashCommandPath: "/agent verify",
            slashCommandStatus: status);
        ChatMessages.Add(vm);
    }

    /// <summary>Активная ветка; иначе основная (не <see cref="Guid.Empty"/> — иначе ломается выбор темы в Skia).</summary>
    private Guid ResolveMessageThreadId() =>
        _activeThreadId != Guid.Empty ? _activeThreadId : _mainThreadId;

    public string ActiveClarificationTitle => _activeClarificationBatch?.Title?.Trim() is { Length: > 0 } title
        ? title
        : "Уточнения к текущему шагу";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private string _chatInput = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendChatCommand))]
    private bool _isChatLoading;

    [ObservableProperty]
    private string _chatLoadingStatusText = "";

    [ObservableProperty]
    private string _clarificationStatusText = "";

    [ObservableProperty]
    private int _selectedMessageIndex = -1;

    /// <summary>Подсветка строк ленты при multi-range select (ADR 0138). Пусто — только <see cref="SelectedMessageIndex"/>.</summary>
    public IReadOnlySet<int> HighlightedMessageIndices { get; private set; } = new HashSet<int>();

    [ObservableProperty]
    private ChatSurfaceSnapshot _chatSurfaceSnapshot = ChatSurfaceSnapshot.Empty;

    /// <summary>Подсказка по активной ветке (короткий id).</summary>
    [ObservableProperty]
    private string _threadBranchHint = "";

    [ObservableProperty]
    private Guid _selectedChatThreadId = Guid.Empty;

    [ObservableProperty]
    private bool _isChatOverviewMode;

    /// <summary>Текущая модель Cursor ACP (после <c>session/new</c>).</summary>
    [ObservableProperty]
    private CursorAcpModelPick? _selectedCursorAcpModelPick;

    partial void OnSelectedMessageIndexChanged(int value)
    {
        RefreshChatSurfaceSnapshot();
        RefreshComposerAutocomplete();
    }

    partial void OnThreadBranchHintChanged(string value)
    {
        RefreshChatSurfaceSnapshot();
    }

    partial void OnIsChatOverviewModeChanged(bool value)
    {
        RefreshChatSurfaceSnapshot();
    }

    private void OnChatMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<ChatMessageViewModel>())
                item.PropertyChanged -= OnChatMessagePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<ChatMessageViewModel>())
                item.PropertyChanged += OnChatMessagePropertyChanged;
        }

        OnPropertyChanged(nameof(HasChatMessages));
        RefreshChatSurfaceSnapshot();
    }

    private void OnChatMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ChatMessageViewModel.Content)
            or nameof(ChatMessageViewModel.SlashCommandStatus))
            RefreshChatSurfaceSnapshot();
    }

    [RelayCommand(CanExecute = nameof(CanSendChat))]
    private Task SendChatAsync() =>
        IntercomOutboundSendOrchestrator.RunAsync(CreateIntercomOutboundSendHost());

    private bool CanSendChat()
    {
        if (string.IsNullOrWhiteSpace(ChatInput))
            return false;

        if (_getChatMcpOnly())
            return true;

        if (string.Equals(_getActiveAiProvider(), "CursorACP", StringComparison.Ordinal))
            return CursorAcpAgentPath.TryResolve(_getCursorAcpAgentPath(), out _, out _);

        return _getActiveAiProvider() != "Ollama"
            || (!string.IsNullOrEmpty(_getSelectedOllamaModel())
                && _getSelectedOllamaModel() != MainWindowViewModel.InstallNewSentinel);
    }
}
