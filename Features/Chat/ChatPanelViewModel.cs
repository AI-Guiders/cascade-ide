using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia.Threading;
using CascadeIDE.Models.AgentChat;
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
    private readonly ChatSessionStore _sessionStore;

    private CursorAcpChatConnection? _cursorAcp;
    private ClarificationBatch? _activeClarificationBatch;
    private Guid _sessionId;

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
        _sessionStore = new ChatSessionStore(_getWorkspaceRoot());
        _sessionId = _sessionStore.EnsureSessionId();
        ChatMessages.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasChatMessages));
        _ = InitializeSessionAsync();
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
    public ObservableCollection<ClarificationDraftItemViewModel> ClarificationDraftItems { get; } = [];

    public bool HasChatMessages => ChatMessages.Count > 0;
    public bool HasActiveClarificationBatch => _activeClarificationBatch is not null;

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
    private string _clarificationStatusText = "";

    public void ShowClarificationBatch(ClarificationBatch batch)
    {
        _activeClarificationBatch = batch;
        ClarificationDraftItems.Clear();
        foreach (var item in batch.Items)
            ClarificationDraftItems.Add(new ClarificationDraftItemViewModel(item));

        ClarificationStatusText = "";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
        _ = PersistEventAsync(
            ChatHistoryEventKind.ClarificationBatchOpened,
            new { batch.Id, batch.Title, Items = batch.Items.Select(x => new { x.Id, x.Prompt, x.AnswerStyle, x.ChoiceOptions }) });
    }

    [RelayCommand(CanExecute = nameof(CanSubmitClarificationResponse))]
    private void SubmitClarificationResponse()
    {
        if (_activeClarificationBatch is null)
            return;

        var answers = ClarificationDraftItems.ToDictionary(x => x.Id, x => x.Answer?.Trim() ?? "", StringComparer.Ordinal);
        var response = new ClarificationResponse(_activeClarificationBatch.Id, answers);
        if (!ClarificationBatchValidation.TryValidate(_activeClarificationBatch, response, out var error))
        {
            ClarificationStatusText = error ?? "Проверь ответы по пунктам.";
            return;
        }

        var payload = string.Join("; ", ClarificationDraftItems.Select(x => $"{x.Id}: {x.Answer?.Trim()}"));
        ChatMessages.Add(new ChatMessageViewModel("user", $"[clarification] {payload}"));
        _activeClarificationBatch = null;
        ClarificationDraftItems.Clear();
        ClarificationStatusText = "Пакет уточнений сохранен в диалог.";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
        _ = PersistEventAsync(ChatHistoryEventKind.ClarificationAnswerSubmitted, new { Answers = answers });
    }

    [RelayCommand(CanExecute = nameof(CanDismissClarificationBatch))]
    private void DismissClarificationBatch()
    {
        _activeClarificationBatch = null;
        ClarificationDraftItems.Clear();
        ClarificationStatusText = "";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSendChat))]
    private async Task SendChatAsync()
    {
        var input = ChatInput.Trim();
        if (string.IsNullOrEmpty(input))
            return;

        ChatInput = "";
        ChatMessages.Add(new ChatMessageViewModel("user", input));
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, new { Role = "user", Content = input });
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
                    _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, new { Role = "assistant", Content = assistantMsg.Content });
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
                _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, new { Role = "assistant", Content = assistantMsg.Content });
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

    private bool CanSubmitClarificationResponse() =>
        _activeClarificationBatch is not null && ClarificationDraftItems.Count > 0 && !IsChatLoading;

    private bool CanDismissClarificationBatch() => _activeClarificationBatch is not null;

    private async Task InitializeSessionAsync()
    {
        try
        {
            await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var events = await _sessionStore.ReadEventsAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            if (events.Count == 0)
                return;
            var recovered = 0;
            foreach (var ev in events)
            {
                if (!string.Equals(ev.Kind, ChatHistoryEventKind.MessageAdded, StringComparison.Ordinal))
                    continue;
                using var doc = JsonDocument.Parse(ev.PayloadJson);
                if (!doc.RootElement.TryGetProperty("Role", out var role) || !doc.RootElement.TryGetProperty("Content", out var content))
                    continue;
                var msg = new ChatMessageViewModel(role.GetString() ?? "assistant", content.GetString() ?? "");
                UiScheduler.Default.Post(() => ChatMessages.Add(msg));
                recovered++;
            }

            if (recovered > 0)
                UiScheduler.Default.Post(() => ClarificationStatusText = $"Восстановлено сообщений: {recovered}");
        }
        catch
        {
            // v1 persistence best-effort: не роняем чат при ошибке диска/JSON.
        }
    }

    private async Task PersistEventAsync(string kind, object payload)
    {
        try
        {
            var ev = new ChatHistoryEvent(
                Guid.NewGuid(),
                _sessionId,
                DateTimeOffset.UtcNow,
                kind,
                JsonSerializer.Serialize(payload));
            await _sessionStore.AppendEventAsync(ev, CancellationToken.None).ConfigureAwait(false);
            var meta = await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            if (meta.UpdatedAtUtc < ev.AtUtc)
                await _sessionStore.SaveMetadataAsync(meta with { UpdatedAtUtc = ev.AtUtc }, CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
            // intentionally ignored (best-effort persistence).
        }
    }
}
