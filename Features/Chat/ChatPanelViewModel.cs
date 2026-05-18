#nullable enable
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Extensions.AI;
using CascadeConversationMessage = CascadeIDE.Services.ChatMessage;
using AgentClientProtocol;
using CascadeIDE.Models;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Services;
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
    private const string CollapsedThinkingPrefix = "[thinking свернут] ";

    private readonly Services.AiProviderManager _aiProviderManager;
    private readonly Func<string> _getActiveAiProvider;
    private readonly Func<string?> _getSelectedOllamaModel;
    private readonly Func<bool> _getChatMcpOnly;
    private readonly Func<bool> _getShowThinkingInHistory;
    private readonly Func<bool> _getUseMinimizedContext;
    private readonly Func<string?> _getCurrentFilePath;
    private readonly Func<string> _getEditorText;
    private readonly Func<string> _getWorkspaceRoot;
    private readonly Func<string> _getCursorAcpAgentPath;
    private readonly Func<string> _getExternalMcpServersJson;
    private readonly Func<bool> _getAcpAutoInjectIdeMcp;
    private readonly Func<string?> _getCursorAcpPreferredModelId;
    private readonly Action<string?>? _onUserSelectedCursorAcpModelId;
    private readonly Action<string>? _appendAcpTerminal;
    private readonly Action? _showAcpTerminal;
    private readonly Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? _executeIdeCommandForMafAgent;
    private readonly ChatSlashCommandRunner _slashCommandRunner;
    private readonly IWorkspaceFileSlashCompletionProvider? _workspaceFileSlashCompletion;
    private readonly ISessionTopicSlashCompletionProvider _sessionTopicSlashCompletion;
    private readonly Func<Uri>? _getLocalOllamaEndpoint;
    private readonly Func<string>? _getEffectiveOllamaModelId;
    private readonly Func<IChatClient?>? _tryCreateCloudMafIChatClient;
    private readonly Func<string?>? _getChatMinimizedContextBlock;
    private readonly Func<string> _getSendMessageKey;
    private readonly ChatSessionStore _sessionStore;
    private readonly Dictionary<Guid, string> _collapsedThinkingByMessageId = new();

    private CursorAcpChatConnection? _cursorAcp;
    private ClarificationBatch? _activeClarificationBatch;
    private Guid _sessionId;
    private Guid _mainThreadId;
    private Guid _activeThreadId;
    private Guid? _pendingParentForNextMessage;
    private int _acpWaitWatchdogGeneration;
    private DateTimeOffset _lastAcpActivityUtc;
    private string _chatLoadingStageBaseText = "";
    private bool _suppressCursorAcpModelPickChanged;

    public ChatPanelViewModel(
        Services.AiProviderManager aiProviderManager,
        Func<string> getActiveAiProvider,
        Func<string?> getSelectedOllamaModel,
        Func<bool> getChatMcpOnly,
        Func<bool> getShowThinkingInHistory,
        Func<bool> getUseMinimizedContext,
        Func<string?> getCurrentFilePath,
        Func<string> getEditorText,
        Func<string> getWorkspaceRoot,
        Func<string> getCursorAcpAgentPath,
        Func<string> getExternalMcpServersJson,
        Func<bool> getAcpAutoInjectIdeMcp,
        Func<string?> getCursorAcpPreferredModelId,
        Action<string?>? onUserSelectedCursorAcpModelId = null,
        Action<string>? appendAcpTerminal = null,
        Action? showAcpTerminal = null,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? executeIdeCommandForMafAgent = null,
        Func<Uri>? getLocalOllamaEndpoint = null,
        Func<string>? getEffectiveOllamaModelId = null,
        Func<IChatClient?>? tryCreateCloudMafIChatClient = null,
        Func<string?>? getChatMinimizedContextBlock = null,
        Func<string>? getSendMessageKey = null,
        Func<string?>? getSolutionPath = null,
        Func<ObservableCollection<SolutionItem>>? getSolutionRoots = null)
    {
        _aiProviderManager = aiProviderManager;
        _getActiveAiProvider = getActiveAiProvider;
        _getSelectedOllamaModel = getSelectedOllamaModel;
        _getChatMcpOnly = getChatMcpOnly;
        _getShowThinkingInHistory = getShowThinkingInHistory;
        _getUseMinimizedContext = getUseMinimizedContext;
        _getCurrentFilePath = getCurrentFilePath;
        _getEditorText = getEditorText;
        _getWorkspaceRoot = getWorkspaceRoot;
        _getCursorAcpAgentPath = getCursorAcpAgentPath;
        _getExternalMcpServersJson = getExternalMcpServersJson;
        _getAcpAutoInjectIdeMcp = getAcpAutoInjectIdeMcp;
        _getCursorAcpPreferredModelId = getCursorAcpPreferredModelId;
        _onUserSelectedCursorAcpModelId = onUserSelectedCursorAcpModelId;
        _appendAcpTerminal = appendAcpTerminal;
        _showAcpTerminal = showAcpTerminal;
        _executeIdeCommandForMafAgent = executeIdeCommandForMafAgent;
        _workspaceFileSlashCompletion = getSolutionPath is not null && getSolutionRoots is not null
            ? new WorkspaceFileSlashCompletionProvider(getSolutionPath, getSolutionRoots, getWorkspaceRoot)
            : null;
        _sessionTopicSlashCompletion = new SessionTopicSlashCompletionProvider(() => ChatSurfaceSnapshot);
        _slashCommandRunner = new ChatSlashCommandRunner(
            executeIdeCommandForMafAgent,
            () => new ChatSlashEditorContext(_getCurrentFilePath?.Invoke(), _getEditorText?.Invoke()),
            getWorkspaceRoot,
            () => ChatSurfaceSnapshot,
            () => SelectedChatThreadId,
            id => SelectedChatThreadId = id,
            v => IsChatOverviewMode = v,
            setTopicPicker: SetTopicPickerPresentation,
            createTopicWithTitle: CreateTopicWithTitle);
        _getLocalOllamaEndpoint = getLocalOllamaEndpoint;
        _getEffectiveOllamaModelId = getEffectiveOllamaModelId;
        _tryCreateCloudMafIChatClient = tryCreateCloudMafIChatClient;
        _getChatMinimizedContextBlock = getChatMinimizedContextBlock;
        _getSendMessageKey = getSendMessageKey ?? (() => "Enter");
        _sessionStore = new ChatSessionStore(_getWorkspaceRoot());
        _sessionId = _sessionStore.EnsureSessionId();
        ChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
        _ = InitializeSessionAsync();
        RefreshChatSurfaceSnapshot();
    }

    /// <summary>Сброс stdio-сессии Cursor ACP (смена провайдера, пути к агенту или корня workspace).</summary>
    public void DisposeCursorAcpSession()
    {
        _cursorAcp?.Dispose();
        _cursorAcp = null;
        void clearPicks()
        {
            _suppressCursorAcpModelPickChanged = true;
            try
            {
                CursorAcpModelPicks.Clear();
                SelectedCursorAcpModelPick = null;
            }
            finally
            {
                _suppressCursorAcpModelPickChanged = false;
            }
        }

        if (UiScheduler.Default.CheckAccess())
            clearPicks();
        else
            UiScheduler.Default.Post(clearPicks);
    }

    /// <summary>Вызвать из главного окна при смене провайдера/модели, влияющих на <see cref="CanSendChat"/>.</summary>
    public void RefreshSendChatCommandState() => SendChatCommand.NotifyCanExecuteChanged();

    /// <summary>Клавиша отправки из настроек (Enter / Ctrl+Enter / Shift+Enter).</summary>
    public string GetSendMessageKey() => _getSendMessageKey();

    public ObservableCollection<ChatMessageViewModel> ChatMessages { get; } = [];
    public ObservableCollection<ClarificationDraftItemViewModel> ClarificationDraftItems { get; } = [];
    public ObservableCollection<CursorAcpModelPick> CursorAcpModelPicks { get; } = [];

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
    private string _chatLoadingStatusText = "";

    [ObservableProperty]
    private string _clarificationStatusText = "";

    [ObservableProperty]
    private int _selectedMessageIndex = -1;

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
    }

    partial void OnThreadBranchHintChanged(string value)
    {
        RefreshChatSurfaceSnapshot();
    }

    partial void OnIsChatOverviewModeChanged(bool value)
    {
        RefreshChatSurfaceSnapshot();
    }

    partial void OnSelectedCursorAcpModelPickChanged(CursorAcpModelPick? value)
    {
        if (_suppressCursorAcpModelPickChanged || value is null || _cursorAcp is null)
            return;
        _ = ApplyUserSelectedCursorAcpModelAsync(value);
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
        RefreshChatSurfaceSnapshot();
        _ = PersistEventAsync(ChatHistoryEventKind.ClarificationBatchOpened, batch);
    }

    public string OpenClarificationBatchFromJson(string batchJson)
    {
        if (string.IsNullOrWhiteSpace(batchJson))
            return "Missing batch_json";

        try
        {
            var batch = JsonSerializer.Deserialize<ClarificationBatch>(batchJson, ChatPanelJson);
            if (batch is null)
                return "Invalid clarification batch";
            ShowClarificationBatch(batch);
            return "OK";
        }
        catch (JsonException ex)
        {
            return $"Invalid clarification batch JSON: {ex.Message}";
        }
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

        ApplyClarificationResponse(response, answers);
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
        RefreshChatSurfaceSnapshot();
    }

    [RelayCommand(CanExecute = nameof(CanSendChat))]
    private async Task SendChatAsync()
    {
        var rawInput = ChatInput.Trim();
        if (string.IsNullOrEmpty(rawInput))
            return;

        var parse = ChatSlashCommandParser.TryParse(rawInput);
        if (parse.IsSlashLine)
        {
            var slashPath = ChatSlashCommandPresentation.FormatDisplayPath(parse, rawInput);
            var slashArgs = ChatSlashCommandPresentation.NormalizeArgsTail(parse.ArgsTail);
            var threadAtSlash = _activeThreadId;
            var cmdMsg = ChatMessageViewModel.CreateSlashCommand(slashPath, slashArgs, threadAtSlash);
            ChatInput = "";
            ChatMessages.Add(cmdMsg);
            _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(cmdMsg));
            RefreshChatSurfaceSnapshot();

            var slash = await _slashCommandRunner.TryRunAsync(rawInput).ConfigureAwait(false);
            await UiScheduler.Default.InvokeAsync(() =>
            {
                if (_activeThreadId != threadAtSlash && _activeThreadId != Guid.Empty)
                    cmdMsg.AssignThread(_activeThreadId);
                cmdMsg.ApplySlashCommandResult(slash);
                _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(cmdMsg));
                RefreshChatSurfaceSnapshot();
            });
            return;
        }

        var input = ApplyProductSpineToOutboundMessage(rawInput);
        if (string.IsNullOrEmpty(input))
            return;

        ChatInput = "";
        var parent = _pendingParentForNextMessage;
        _pendingParentForNextMessage = null;
        var userMsg = new ChatMessageViewModel("user", input, threadId: _activeThreadId, parentMessageId: parent);
        ChatMessages.Add(userMsg);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(userMsg));
        IsChatLoading = true;
        ChatLoadingStatusText = "Модель отвечает…";

        try
        {
            if (_getChatMcpOnly())
                return;

            if (string.Equals(_getActiveAiProvider(), "CursorACP", StringComparison.Ordinal))
                await SendChatWithCursorAcpAsync(input).ConfigureAwait(false);
            else
                await SendChatWithStreamingProviderAsync(input).ConfigureAwait(false);
        }
        finally
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                StopAcpWaitWatchdog();
                IsChatLoading = false;
                ChatLoadingStatusText = "";
            });
        }
    }

    private async Task SendChatWithCursorAcpAsync(string input)
    {
        var assistantMsg = new ChatMessageViewModel("assistant", "", threadId: _activeThreadId);
        ChatMessages.Add(assistantMsg);
        ChatMessageViewModel? thoughtMsg = null;
        ChatMessageViewModel? toolMsg = null;
        try
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                SetChatLoadingStage("Подключение к Cursor ACP…");
                MarkAcpActivity();
                RestartAcpWaitWatchdog();
            });
            _cursorAcp ??= new CursorAcpChatConnection();
            _cursorAcp.SetIdeTerminalCallbacks(
                text =>
                {
                    _appendAcpTerminal?.Invoke(text);
                    UiScheduler.Default.Post(() =>
                    {
                        SetChatLoadingStage("Выполняю инструмент…");
                        MarkAcpActivity();
                    });
                },
                _showAcpTerminal);
            var workspace = _getWorkspaceRoot().Trim();
            if (string.IsNullOrEmpty(workspace))
                workspace = Environment.CurrentDirectory;
            await _cursorAcp.PromptAsync(
                workspace,
                _getCursorAcpAgentPath(),
                _getExternalMcpServersJson(),
                _getAcpAutoInjectIdeMcp(),
                _getCursorAcpPreferredModelId(),
                input,
                appendMessageChunk: t => UiScheduler.Default.Post(() =>
                {
                    assistantMsg.Content += t;
                    SetChatLoadingStage("Формирую ответ…");
                    MarkAcpActivity();
                }),
                appendThoughtChunk: t => UiScheduler.Default.Post(() =>
                {
                    thoughtMsg ??= CreateThoughtMessage();
                    thoughtMsg.Content += t;
                    SetChatLoadingStage("Модель думает…");
                    MarkAcpActivity();
                }),
                onStage: stage => UiScheduler.Default.Post(() =>
                {
                    if (stage == CursorAcpStreamStage.ToolCall)
                        toolMsg ??= CreateToolMessage();
                    SetChatLoadingStage(stage switch
                    {
                        CursorAcpStreamStage.ThoughtChunk => "Модель думает…",
                        CursorAcpStreamStage.ToolCall => "Выполняю инструмент…",
                        _ => "Формирую ответ…"
                    });
                    MarkAcpActivity();
                }),
                onSessionModels: state => UiScheduler.Default.Post(() => ApplyCursorAcpSessionModels(state)),
                CancellationToken.None).ConfigureAwait(false);
            await UiScheduler.Default.InvokeAsync(() =>
            {
                FinalizeThinkingMessage(thoughtMsg);
                FinalizeToolMessage(toolMsg, isError: false);
            });
            _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(assistantMsg));
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                var mapped = MapCursorAcpError(ex);
                assistantMsg.Content = mapped.UserMessage;
                FinalizeThinkingMessage(thoughtMsg);
                FinalizeToolMessage(toolMsg, isError: true);
                SetChatLoadingStage(mapped.StageText);
            });
        }
    }

    private async Task SendChatWithStreamingProviderAsync(string input)
    {
        if (TryResolveMafIdeChat(out var exec, out var chatClient))
        {
            try
            {
                await SendChatWithMafIdeAgentAsync(input, exec!, chatClient!).ConfigureAwait(false);
            }
            finally
            {
                chatClient?.Dispose();
            }

            return;
        }

        var messages = ChatMessages.Take(ChatMessages.Count - 1)
            .Select(m => new Services.ChatMessage(m.Role, m.Content))
            .Append(new Services.ChatMessage("user", input))
            .ToList();
        var assistantMsg = new ChatMessageViewModel("assistant", "", threadId: _activeThreadId);
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
        _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(assistantMsg));
    }

    private bool TryResolveMafIdeChat(
        [NotNullWhen(true)] out Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? exec,
        [NotNullWhen(true)] out IChatClient? chatClient)
    {
        exec = null;
        chatClient = null;

        var handler = _executeIdeCommandForMafAgent;
        if (handler is null)
            return false;

        var key = _getActiveAiProvider();
        if (string.Equals(key, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = _getLocalOllamaEndpoint?.Invoke();
            var modelId = _getEffectiveOllamaModelId?.Invoke()?.Trim();
            if (endpoint is null || string.IsNullOrWhiteSpace(modelId))
                return false;

            exec = handler;
            chatClient = new OllamaChatClient(endpoint, modelId);
            return true;
        }

        if (string.Equals(key, "Anthropic", StringComparison.Ordinal)
            || string.Equals(key, "OpenAI", StringComparison.Ordinal)
            || string.Equals(key, "DeepSeek", StringComparison.Ordinal))
        {
            var cloud = _tryCreateCloudMafIChatClient?.Invoke();
            if (cloud is null)
                return false;

            exec = handler;
            chatClient = cloud;
            return true;
        }

        return false;
    }

    /// <summary>Microsoft Agent Framework + <see cref="IChatClient"/> (Ollama / облако) и вызовы IDE через <c>ExecuteCommandAsync</c> (как MCP).</summary>
    private async Task SendChatWithMafIdeAgentAsync(
        string input,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> executeIdeCommandAsync,
        IChatClient chatClient)
    {
        var dialogMessages = ChatMessages.Take(ChatMessages.Count - 1)
            .Where(m => IsUserOrAssistantOrToolForMafHistory(m.Role))
            .Select(m => new CascadeConversationMessage(m.Role, m.Content))
            .Append(new CascadeConversationMessage("user", input))
            .ToList();

        var minimized = _getChatMinimizedContextBlock?.Invoke();
        minimized = string.IsNullOrWhiteSpace(minimized) ? null : minimized.Trim();

        var projectRules = CascadeIdeMafProjectAgentRules.TryLoadMerged(_getWorkspaceRoot());

        ChatMessageViewModel? assistantMsg = null;

        try
        {
            var (text, toolUiBubbles) = await CascadeIdeMafIdeAgentChat.RunAsync(
                chatClient,
                dialogMessages,
                minimized,
                projectRules,
                executeIdeCommandAsync,
                CancellationToken.None).ConfigureAwait(false);

            await UiScheduler.Default.InvokeAsync(() =>
            {
                foreach (var bubble in toolUiBubbles)
                {
                    ChatMessages.Add(new ChatMessageViewModel(
                        "tool",
                        bubble,
                        threadId: _activeThreadId));
                }

                assistantMsg = new ChatMessageViewModel("assistant", text, threadId: _activeThreadId);
                ChatMessages.Add(assistantMsg);
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await UiScheduler.Default.InvokeAsync(() =>
            {
                assistantMsg = new ChatMessageViewModel(
                    "assistant",
                    $"Ошибка агента (MAF): {ex.Message}",
                    threadId: _activeThreadId);
                ChatMessages.Add(assistantMsg);
            }).ConfigureAwait(false);
        }

        if (assistantMsg is not null)
            _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(assistantMsg));
    }

    private static bool IsUserOrAssistantRole(string role)
        => string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)
           || string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase);

    /// <summary>Роли, которые уходят в <see cref="CascadeIdeMafIdeAgentChat.RunAsync"/> как история (в т.ч. <c>tool</c> → <see cref="Microsoft.Extensions.AI.ChatRole.Tool"/> с усечением в сборщике сообщений).</summary>
    private static bool IsUserOrAssistantOrToolForMafHistory(string role)
        => IsUserOrAssistantRole(role)
           || string.Equals(role, "tool", StringComparison.OrdinalIgnoreCase);

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

    private void SetChatLoadingStage(string stageText)
    {
        _chatLoadingStageBaseText = stageText;
        ChatLoadingStatusText = stageText;
    }

    private void MarkAcpActivity() => _lastAcpActivityUtc = DateTimeOffset.UtcNow;

    private void RestartAcpWaitWatchdog()
    {
        var generation = ++_acpWaitWatchdogGeneration;
        _ = Task.Run(async () =>
        {
            while (generation == _acpWaitWatchdogGeneration)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
                catch
                {
                    return;
                }

                var elapsed = DateTimeOffset.UtcNow - _lastAcpActivityUtc;
                if (elapsed < TimeSpan.FromSeconds(8))
                    continue;
                await UiScheduler.Default.InvokeAsync(() =>
                {
                    if (!IsChatLoading || generation != _acpWaitWatchdogGeneration)
                        return;
                    var seconds = Math.Max(8, (int)elapsed.TotalSeconds);
                    ChatLoadingStatusText = $"{_chatLoadingStageBaseText} Ждём ответ… {seconds}с";
                });
            }
        });
    }

    private void StopAcpWaitWatchdog() => _acpWaitWatchdogGeneration++;

    private void ApplyCursorAcpSessionModels(SessionModelState? state)
    {
        if (state?.AvailableModels is not { Length: > 0 } models)
        {
            _suppressCursorAcpModelPickChanged = true;
            try
            {
                CursorAcpModelPicks.Clear();
                SelectedCursorAcpModelPick = null;
            }
            finally
            {
                _suppressCursorAcpModelPickChanged = false;
            }

            return;
        }

        _suppressCursorAcpModelPickChanged = true;
        try
        {
            CursorAcpModelPicks.Clear();
            foreach (var m in models)
            {
                var label = string.IsNullOrWhiteSpace(m.Description)
                    ? m.Name
                    : $"{m.Name} — {m.Description}";
                CursorAcpModelPicks.Add(new CursorAcpModelPick(m.ModelId, label));
            }

            var currentId = state.CurrentModelId;
            SelectedCursorAcpModelPick = CursorAcpModelPicks.FirstOrDefault(p =>
                string.Equals(p.ModelId, currentId, StringComparison.Ordinal))
                ?? CursorAcpModelPicks[0];
        }
        finally
        {
            _suppressCursorAcpModelPickChanged = false;
        }
    }

    private async Task ApplyUserSelectedCursorAcpModelAsync(CursorAcpModelPick pick)
    {
        if (_cursorAcp is null)
            return;
        try
        {
            var ok = await _cursorAcp.TrySetSessionModelAsync(pick.ModelId, CancellationToken.None).ConfigureAwait(false);
            if (!ok)
                return;
            await UiScheduler.Default.InvokeAsync(() => _onUserSelectedCursorAcpModelId?.Invoke(pick.ModelId));
        }
        catch
        {
            // сессия может быть сброшена параллельно
        }
    }

    private static (string UserMessage, string StageText) MapCursorAcpError(Exception ex)
    {
        var message = ex.Message?.Trim() ?? "Неизвестная ошибка.";
        if (ContainsAny(message, "upgrade", "plan", "billing", "quota", "rate limit", "credits"))
        {
            return (
                "[Cursor ACP / provider-limit] Доступ к модели ограничен тарифом или квотой. Проверь план/биллинг в Cursor, либо выбери другую модель.",
                "Ошибка провайдера (план/квота)");
        }

        if (ContainsAny(message, "timeout", "timed out", "deadline", "network", "connection"))
        {
            return (
                "[Cursor ACP / network] Не удалось дождаться ответа от провайдера. Попробуй повторить запрос.",
                "Сетевая ошибка провайдера");
        }

        return ($"[Cursor ACP] {message}", "Ошибка ACP");
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string BuildCollapsedThinkingPreview(string fullThinking)
    {
        var preview = fullThinking.Length <= 180 ? fullThinking : fullThinking[..180].TrimEnd() + "…";
        return CollapsedThinkingPrefix + preview;
    }

    private bool CanSendChat()
    {
        if (string.IsNullOrWhiteSpace(ChatInput) || IsChatLoading)
            return false;

        if (_getChatMcpOnly())
            return true;

        if (string.Equals(_getActiveAiProvider(), "CursorACP", StringComparison.Ordinal))
            return CursorAcpAgentPath.TryResolve(_getCursorAcpAgentPath(), out _, out _);

        return _getActiveAiProvider() != "Ollama"
            || (!string.IsNullOrEmpty(_getSelectedOllamaModel())
                && _getSelectedOllamaModel() != MainWindowViewModel.InstallNewSentinel);
    }

    private bool CanSubmitClarificationResponse() =>
        _activeClarificationBatch is not null && ClarificationDraftItems.Count > 0 && !IsChatLoading;

    private bool CanDismissClarificationBatch() => _activeClarificationBatch is not null;

    /// <summary>Добавить сообщение из внешнего MCP (<c>send_chat</c> с <c>role=assistant</c>).</summary>
    public string AppendMessageFromMcp(string role, string content)
    {
        var r = string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
        var trimmed = content.Trim();
        if (trimmed.Length == 0)
            return "Empty content";
        var vm = new ChatMessageViewModel(r, trimmed, threadId: _activeThreadId);
        ChatMessages.Add(vm);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(vm));
        if (string.Equals(r, "assistant", StringComparison.Ordinal))
            _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(vm));
        return "OK";
    }

    public string SubmitClarificationResponseFromJson(string responseJson)
    {
        if (_activeClarificationBatch is null)
            return "No active clarification batch";
        if (string.IsNullOrWhiteSpace(responseJson))
            return "Missing response_json";

        try
        {
            var response = JsonSerializer.Deserialize<ClarificationResponse>(responseJson, ChatPanelJson);
            if (response is null)
                return "Invalid clarification response";
            if (response.BatchId != _activeClarificationBatch.Id)
                return "Batch mismatch";
            if (!ClarificationBatchValidation.TryValidate(_activeClarificationBatch, response, out var error))
                return error ?? "Invalid clarification response";

            ApplyClarificationResponse(response, response.AnswersByItemId);
            return "OK";
        }
        catch (JsonException ex)
        {
            return $"Invalid clarification response JSON: {ex.Message}";
        }
    }

    public string SelectMessageByIndex(int index)
    {
        if (index < 0 || index >= ChatMessages.Count)
            return $"Index out of range: {index}. Count={ChatMessages.Count}.";
        SelectedMessageIndex = index;
        return "OK";
    }

    /// <summary>Сдвинуть выбор в ленте сообщений на delta (-1/ +1) для keyboard-first сценария.</summary>
    public string SelectMessageByOffset(int delta)
    {
        if (ChatMessages.Count == 0)
            return "No messages";
        var current = SelectedMessageIndex;
        if (current < 0)
            current = delta >= 0 ? 0 : ChatMessages.Count - 1;
        var next = Math.Clamp(current + delta, 0, ChatMessages.Count - 1);
        SelectedMessageIndex = next;
        return "OK";
    }

    /// <summary>Сдвинуть выбор темы в overview по циклу.</summary>
    public string NavigateThreadSelection(int delta)
    {
        var threads = ChatSurfaceSnapshot.Layout.Overview;
        if (threads.Count == 0)
            return "No threads";
        var current = -1;
        for (var i = 0; i < threads.Count; i++)
        {
            if (threads[i].ThreadId == SelectedChatThreadId)
            {
                current = i;
                break;
            }
        }
        if (current < 0)
        {
            for (var i = 0; i < threads.Count; i++)
            {
                if (!threads[i].IsActive)
                    continue;
                current = i;
                break;
            }
        }
        if (current < 0)
            current = 0;
        var next = (current + delta) % threads.Count;
        if (next < 0)
            next += threads.Count;
        SelectedChatThreadId = threads[next].ThreadId;
        return "OK";
    }

    public string OpenSelectedThreadDetail()
    {
        if (SelectedChatThreadId == Guid.Empty)
            return "No selected thread";
        IsChatOverviewMode = false;
        return "OK";
    }

    public string ShowThreadOverview()
    {
        IsChatOverviewMode = true;
        return "OK";
    }

    /// <summary>Переключить выбранный thinking-блок между свёрнутым и полным видом.</summary>
    public string ToggleSelectedThinkingDetails()
    {
        if (SelectedMessageIndex < 0 || SelectedMessageIndex >= ChatMessages.Count)
            return "No selected message";
        var selected = ChatMessages[SelectedMessageIndex];
        if (!string.Equals(selected.Role, "thinking", StringComparison.OrdinalIgnoreCase))
            return "Selected message is not thinking";
        if (!_collapsedThinkingByMessageId.TryGetValue(selected.MessageId, out var full))
            return "Thinking message has no stored details";

        if (selected.Content.StartsWith(CollapsedThinkingPrefix, StringComparison.Ordinal))
            selected.Content = full;
        else
            selected.Content = BuildCollapsedThinkingPreview(full);
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
            thread_id = m.ThreadId.ToString("N"),
            parent_message_id = m.ParentMessageId?.ToString("N"),
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
                ChatHistoryPayloadMapping.ToMessageEditedPayload(messageId, newContent, reason));
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

    private void ApplyClarificationResponse(ClarificationResponse response, IReadOnlyDictionary<string, string> answers)
    {
        var clarifyMsg = new ChatMessageViewModel(
            "user",
            BuildClarificationTranscriptMessage(_activeClarificationBatch, answers),
            threadId: _activeThreadId);
        ChatMessages.Add(clarifyMsg);
        _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(clarifyMsg));
        _ = PersistEventAsync(
            ChatHistoryEventKind.ClarificationAnswerSubmitted,
            ChatHistoryPayloadMapping.ToClarificationAnswerPayload(response, answers));

        _activeClarificationBatch = null;
        ClarificationDraftItems.Clear();
        ClarificationStatusText = "Пакет уточнений сохранен в диалог.";
        OnPropertyChanged(nameof(HasActiveClarificationBatch));
        OnPropertyChanged(nameof(ActiveClarificationTitle));
        SubmitClarificationResponseCommand.NotifyCanExecuteChanged();
        DismissClarificationBatchCommand.NotifyCanExecuteChanged();
        RefreshChatSurfaceSnapshot();
    }

    private static string BuildClarificationTranscriptMessage(
        ClarificationBatch? batch,
        IReadOnlyDictionary<string, string> answers)
    {
        var title = string.IsNullOrWhiteSpace(batch?.Title) ? "Уточнения" : batch!.Title.Trim();
        var lines = new List<string> { $"{title}:", "" };
        foreach (var pair in answers)
            lines.Add($"- {pair.Key}: {pair.Value}");
        return string.Join(Environment.NewLine, lines);
    }
}
