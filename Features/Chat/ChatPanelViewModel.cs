using System.Collections.ObjectModel;
using Avalonia.Threading;
using CascadeIDE.Services.CursorAcp;
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
    private readonly Func<string> _getWorkspaceRoot;
    private readonly Func<string> _getCursorAcpAgentPath;
    private readonly Action<string>? _appendAcpTerminal;
    private readonly Action? _showAcpTerminal;

    private CursorAcpChatConnection? _cursorAcp;

    public ChatPanelViewModel(
        Services.AiProviderManager aiProviderManager,
        Func<string> getActiveAiProvider,
        Func<string?> getSelectedOllamaModel,
        Func<bool> getUseMinimizedContext,
        Func<string?> getCurrentFilePath,
        Func<string> getEditorText,
        Func<string> getWorkspaceRoot,
        Func<string> getCursorAcpAgentPath,
        Action<string>? appendAcpTerminal = null,
        Action? showAcpTerminal = null)
    {
        _aiProviderManager = aiProviderManager;
        _getActiveAiProvider = getActiveAiProvider;
        _getSelectedOllamaModel = getSelectedOllamaModel;
        _getUseMinimizedContext = getUseMinimizedContext;
        _getCurrentFilePath = getCurrentFilePath;
        _getEditorText = getEditorText;
        _getWorkspaceRoot = getWorkspaceRoot;
        _getCursorAcpAgentPath = getCursorAcpAgentPath;
        _appendAcpTerminal = appendAcpTerminal;
        _showAcpTerminal = showAcpTerminal;
        ChatMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChatMessages));
    }

    /// <summary>Сброс stdio-сессии Cursor ACP (смена провайдера, пути к агенту или корня workspace).</summary>
    public void DisposeCursorAcpSession()
    {
        _cursorAcp?.Dispose();
        _cursorAcp = null;
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
            if (string.Equals(_getActiveAiProvider(), "CursorACP", StringComparison.Ordinal))
            {
                var assistantMsg = new ChatMessageViewModel("assistant", "");
                ChatMessages.Add(assistantMsg);
                try
                {
                    _cursorAcp ??= new CursorAcpChatConnection();
                    _cursorAcp.SetIdeTerminalCallbacks(_appendAcpTerminal, _showAcpTerminal);
                    var workspace = _getWorkspaceRoot().Trim();
                    if (string.IsNullOrEmpty(workspace))
                        workspace = Environment.CurrentDirectory;
                    await _cursorAcp.PromptAsync(
                        workspace,
                        _getCursorAcpAgentPath(),
                        input,
                        t => UiScheduler.Default.Post(() => assistantMsg.Content += t),
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await UiScheduler.Default.InvokeAsync(() =>
                        assistantMsg.Content = "[Cursor ACP] " + ex.Message);
                }
            }
            else
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
                    UiScheduler.Default.Post(() => assistantMsg.Content += t);
                }
            }
        }
        finally
        {
            await UiScheduler.Default.InvokeAsync(() => IsChatLoading = false);
        }
    }

    private bool CanSendChat()
    {
        if (string.IsNullOrWhiteSpace(ChatInput) || IsChatLoading)
            return false;

        if (string.Equals(_getActiveAiProvider(), "CursorACP", StringComparison.Ordinal))
            return CursorAcpAgentPath.TryResolve(_getCursorAcpAgentPath(), out _, out _);

        return _getActiveAiProvider() != "Ollama"
            || (!string.IsNullOrEmpty(_getSelectedOllamaModel())
                && _getSelectedOllamaModel() != MainWindowViewModel.InstallNewSentinel);
    }
}
