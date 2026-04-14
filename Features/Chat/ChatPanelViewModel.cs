using System.Collections.ObjectModel;
using System.IO;
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
    private static readonly JsonSerializerOptions ChatPanelJson = new(JsonSerializerDefaults.Web);

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
    private bool _useSkiaSurface = true;

    [ObservableProperty]
    private string _clarificationStatusText = "";

    [ObservableProperty]
    private int _selectedMessageIndex = -1;

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
        var clarifyMsg = new ChatMessageViewModel("user", $"[clarification] {payload}");
        ChatMessages.Add(clarifyMsg);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, MessageSnapshot(clarifyMsg));
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
        var userMsg = new ChatMessageViewModel("user", input);
        ChatMessages.Add(userMsg);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, MessageSnapshot(userMsg));
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
                    _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, MessageSnapshot(assistantMsg));
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
                _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, MessageSnapshot(assistantMsg));
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

    public string SelectMessageByIndex(int index)
    {
        if (index < 0 || index >= ChatMessages.Count)
            return $"Index out of range: {index}. Count={ChatMessages.Count}.";
        SelectedMessageIndex = index;
        return "OK";
    }

    public string GetSelectedMessageJson()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= ChatMessages.Count)
            return "{\"selected_index\":-1,\"has_selection\":false}";
        var m = ChatMessages[SelectedMessageIndex];
        var role = m.Role ?? "";
        var content = m.Content ?? "";
        return JsonSerializer.Serialize(new
        {
            selected_index = SelectedMessageIndex,
            has_selection = true,
            message_id = m.MessageId.ToString("N"),
            role,
            content
        }, ChatPanelJson);
    }

    /// <summary>Редактирование только ответа ассистента; в лог добавляется <see cref="ChatHistoryEventKind.MessageEdited"/>.</summary>
    public string EditAssistantMessageById(Guid messageId, string newContent, string? reason)
    {
        foreach (var m in ChatMessages)
        {
            if (m.MessageId != messageId)
                continue;
            if (!string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase))
                return JsonSerializer.Serialize(new { ok = false, error = "only_assistant_supported" }, ChatPanelJson);

            m.Content = newContent;
            _ = PersistEventAsync(
                ChatHistoryEventKind.MessageEdited,
                new Dictionary<string, object?>
                {
                    ["message_id"] = messageId.ToString("N"),
                    ["new_content"] = newContent,
                    ["reason"] = string.IsNullOrWhiteSpace(reason) ? "correction" : reason.Trim()
                });
            return JsonSerializer.Serialize(new { ok = true, message_id = messageId.ToString("N") }, ChatPanelJson);
        }

        return JsonSerializer.Serialize(new { ok = false, error = "message_not_found" }, ChatPanelJson);
    }

    /// <summary>Читаемый Markdown текущего чата; опционально запись в .cascade-ide/chat-sessions/exports/.</summary>
    public string ExportReadableMarkdown(bool writeFile, string? fileName)
    {
        var md = ChatReadableExporter.BuildMarkdown(_sessionId, [.. ChatMessages]);
        if (!writeFile)
            return JsonSerializer.Serialize(new { ok = true, markdown = md, relative_path = (string?)null }, ChatPanelJson);

        try
        {
            var ws = _getWorkspaceRoot().Trim();
            if (string.IsNullOrEmpty(ws))
                ws = Environment.CurrentDirectory;
            var dir = Path.Combine(ws, ".cascade-ide", "chat-sessions", "exports");
            Directory.CreateDirectory(dir);
            var name = string.IsNullOrWhiteSpace(fileName) ? $"session-{_sessionId:N}.readable.md" : fileName.Trim();
            if (!name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                name += ".md";
            var safe = Path.GetFileName(name);
            var full = Path.Combine(dir, safe);
            File.WriteAllText(full, md, System.Text.Encoding.UTF8);
            var relative = Path.GetRelativePath(ws, full);
            return JsonSerializer.Serialize(new { ok = true, markdown = md, relative_path = relative }, ChatPanelJson);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { ok = false, markdown = md, error = ex.Message }, ChatPanelJson);
        }
    }

    private static Dictionary<string, object?> MessageSnapshot(ChatMessageViewModel m) => new()
    {
        ["message_id"] = m.MessageId.ToString("N"),
        ["role"] = m.Role,
        ["content"] = m.Content
    };

    private async Task InitializeSessionAsync()
    {
        try
        {
            await _sessionStore.LoadOrCreateMetadataAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var events = await _sessionStore.ReadEventsAsync(_sessionId, CancellationToken.None).ConfigureAwait(false);
            var rows = ChatHistoryMessageProjector.Project(events);
            if (rows.Count == 0)
                return;
            UiScheduler.Default.Post(() =>
            {
                foreach (var row in rows)
                    ChatMessages.Add(new ChatMessageViewModel(row.Role, row.Content, row.MessageId));
                ClarificationStatusText = $"Восстановлено сообщений: {rows.Count}";
            });
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
                JsonSerializer.Serialize(payload, ChatPanelJson));
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
