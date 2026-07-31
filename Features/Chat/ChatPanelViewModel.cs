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
using CascadeIDE.Features.Chat.Application;
using CascadeIDE.Features.Chat.DataAcquisition;
using CascadeIDE.Features.Cockpit;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services;
using CascadeIDE.Services.Intercom;
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
    private const string CollapsedThinkingPrefix = ChatMessageBodyPresentation.CollapsedThinkingPrefix;

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
    private readonly Func<AttachmentAnchor, bool, CancellationToken, Task<string>>? _revealIntercomAttachmentInIde;
    private readonly AnchorDraftPreviewCoordinator? _anchorDraftPreview;
    private readonly ChatSlashCommandRunner _slashCommandRunner;
    private readonly IWorkspaceFileSlashCompletionProvider? _workspaceFileSlashCompletion;
    private readonly ISessionTopicSlashCompletionProvider _sessionTopicSlashCompletion;
    private readonly IMessageAnchorSlashCompletionProvider _messageAnchorSlashCompletion;
    private readonly Func<Uri>? _getLocalOllamaEndpoint;
    private readonly Func<string>? _getEffectiveOllamaModelId;
    private readonly Func<IChatClient?>? _tryCreateCloudMafIChatClient;
    private readonly Func<string?>? _getChatMinimizedContextBlock;
    private readonly Func<string> _getSendMessageKey;
    private readonly Func<string> _getComposerNewLineKey;
    private readonly Func<string?>? _getSolutionPath;
    private readonly Func<int?>? _getEditorSelectionStart;
    private readonly Func<int?>? _getEditorSelectionLength;
    private readonly Func<int?>? _getEditorCaretOffset;
    private readonly ChatSessionStore _sessionStore;
    private readonly Dictionary<Guid, string> _collapsedThinkingByMessageId = new();

    private CursorAcpChatConnection? _cursorAcp;
    private ClarificationBatch? _activeClarificationBatch;
    private Guid _sessionId;
    private string? _sessionSolutionPathRelative;
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
        Func<AttachmentAnchor, bool, CancellationToken, Task<string>>? revealIntercomAttachmentInIde = null,
        Func<Uri>? getLocalOllamaEndpoint = null,
        Func<string>? getEffectiveOllamaModelId = null,
        Func<IChatClient?>? tryCreateCloudMafIChatClient = null,
        Func<string?>? getChatMinimizedContextBlock = null,
        Func<string>? getSendMessageKey = null,
        Func<string>? getComposerNewLineKey = null,
        Func<string?>? getSolutionPath = null,
        Func<ObservableCollection<SolutionItem>>? getSolutionRoots = null,
        Func<int?>? getEditorSelectionStart = null,
        Func<int?>? getEditorSelectionLength = null,
        Func<int?>? getEditorCaretOffset = null,
        Func<string?, int, int, Task>? revealAgentRangeInEditor = null,
        Action<string?>? clearAgentRevealInEditor = null,
        SlashCommandPreviewService? slashCommandPreviewService = null,
        Features.Agent.Environment.IAgentEnvironmentService? agentEnvironment = null,
        Func<string?>? getSolutionPathForAgent = null)
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
        _revealIntercomAttachmentInIde = revealIntercomAttachmentInIde;
        _anchorDraftPreview = revealAgentRangeInEditor is null
            ? null
            : new AnchorDraftPreviewCoordinator(
                () => _getCurrentFilePath?.Invoke(),
                getWorkspaceRoot,
                () => ResolveAttachSolutionPath(),
                ResolveAttachIndexDirectoryRelative,
                revealAgentRangeInEditor,
                clearAgentRevealInEditor);
        _workspaceFileSlashCompletion = getSolutionPath is not null && getSolutionRoots is not null
            ? new WorkspaceFileSlashCompletionProvider(getSolutionPath, getSolutionRoots, getWorkspaceRoot)
            : null;
        _sessionTopicSlashCompletion = new SessionTopicSlashCompletionProvider(() => ChatSurfaceSnapshot);
        _messageAnchorSlashCompletion = new MessageAnchorSlashCompletionProvider(GetSelectedMessageAttachmentsForSlash);
        _slashCommandRunner = new ChatSlashCommandRunner(
            executeIdeCommandForMafAgent,
            () => new ChatSlashEditorContext(
                _getCurrentFilePath?.Invoke(),
                _getEditorText?.Invoke(),
                _getEditorSelectionStart?.Invoke(),
                _getEditorSelectionLength?.Invoke(),
                _getEditorCaretOffset?.Invoke()),
            getWorkspaceRoot,
            () => ChatSurfaceSnapshot,
            () => SelectedChatThreadId,
            id => SelectedChatThreadId = id,
            v => IsChatOverviewMode = v,
            setTopicPicker: SetTopicPickerPresentation,
            createTopicWithTitle: CreateTopicWithTitle,
            renameTopicWithTitle: (threadId, title) =>
                RenameTopicWithTitle(title, threadId == Guid.Empty ? null : threadId),
            tryAttachSlash: TryExecuteAttachSlash,
            selectMessageByOrdinalRangeInDetailLane: SelectMessageByOrdinalRangeInDetailLane,
            selectMessagesByOrdinalRangesInDetailLane: SelectMessagesByOrdinalRangesInDetailLane,
            clearMessageSelectionInDetailLane: ClearMessageSelectionInDetailLane,
            findMessagesForCodeRef: FindMessagesForCodeRef,
            relateMessageRangeToCodeRef: RelateMessageRangeToCodeRef,
            listMessageAnchors: ListAnchorsForSlashContext,
            peekAnchorById: PeekAnchorById,
            agentEnvironment: agentEnvironment,
            getSolutionPathForAgent: getSolutionPathForAgent);
        _slashCommandPreviewService = slashCommandPreviewService
            ?? new SlashCommandPreviewService(tryBuildAnchorSlashPreview);
        _cockpitCommandLineSession = new CockpitCommandLineSession(this, _slashCommandPreviewService);
        _getLocalOllamaEndpoint = getLocalOllamaEndpoint;
        _getEffectiveOllamaModelId = getEffectiveOllamaModelId;
        _tryCreateCloudMafIChatClient = tryCreateCloudMafIChatClient;
        _getChatMinimizedContextBlock = getChatMinimizedContextBlock;
        _getSendMessageKey = getSendMessageKey ?? (() => "Enter");
        _getComposerNewLineKey = getComposerNewLineKey
            ?? (() => ChatComposerChordOptions.ComplementaryChord(_getSendMessageKey()));
        _getSolutionPath = getSolutionPath;
        _getEditorSelectionStart = getEditorSelectionStart;
        _getEditorSelectionLength = getEditorSelectionLength;
        _getEditorCaretOffset = getEditorCaretOffset;
        _sessionStore = new ChatSessionStore(null);
        _sessionId = Guid.Empty;
        ChatMessages.CollectionChanged += OnChatMessagesCollectionChanged;
        _ = ReloadIntercomSessionFromDiskAsync();
        RefreshChatSurfaceSnapshot();
    }

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

    private async Task SendChatWithStreamingProviderAsync(string agentInput, string displayInput)
    {
        if (TryResolveMafIdeChat(out var exec, out var chatClient))
        {
            try
            {
                await SendChatWithMafIdeAgentAsync(agentInput, exec!, chatClient!).ConfigureAwait(false);
            }
            finally
            {
                chatClient?.Dispose();
            }

            return;
        }

        var messages = ChatMessages.Take(ChatMessages.Count - 1)
            .Where(m => !m.IsLocalSelfOnly)
            .Select(m => new Services.ChatMessage(m.Role, m.Content))
            .Append(new Services.ChatMessage("user", agentInput))
            .ToList();
        var assistantMsg = new ChatMessageViewModel("assistant", "", threadId: _activeThreadId);
        ChatMessages.Add(assistantMsg);

        var usageCollector = new ChatTurnUsageCollector();
        await foreach (var token in _aiProviderManager.StreamChatAsync(
            _getActiveAiProvider(),
            messages,
            _getCurrentFilePath(),
            _getEditorText(),
            _getUseMinimizedContext(),
            BeginAgentTurnCancellation(),
            usageCollector))
        {
            var t = token;
            UiScheduler.Default.Post(() => assistantMsg.Content += t);
        }

        await RecordFmTurnUsageAsync(usageCollector.LastTurn).ConfigureAwait(false);
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
            .Where(m => !m.IsLocalSelfOnly)
            .Where(m => IsUserOrAssistantOrToolForMafHistory(m.Role))
            .Select(m => new CascadeConversationMessage(m.Role, m.Content))
            .Append(new CascadeConversationMessage("user", input))
            .ToList();

        var minimized = _getChatMinimizedContextBlock?.Invoke();
        minimized = string.IsNullOrWhiteSpace(minimized) ? null : minimized.Trim();

        var pendingHarness = Harness.TryConsumePendingAgentContext();
        if (!string.IsNullOrWhiteSpace(pendingHarness))
        {
            minimized = string.IsNullOrWhiteSpace(minimized)
                ? pendingHarness.Trim()
                : pendingHarness.Trim() + "\n\n---\n\n" + minimized;
        }

        var projectRules = CascadeIdeMafProjectAgentRules.TryLoadMerged(_getWorkspaceRoot());

        ChatMessageViewModel? assistantMsg = null;

        try
        {
            var (text, toolUiBubbles, fmUsage) = await CascadeIdeMafIdeAgentChat.RunAsync(
                chatClient,
                dialogMessages,
                minimized,
                projectRules,
                executeIdeCommandAsync,
                BeginAgentTurnCancellation()).ConfigureAwait(false);

            await RecordFmTurnUsageAsync(fmUsage).ConfigureAwait(false);

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

    private static string BuildCollapsedThinkingPreview(string fullThinking)
    {
        var preview = fullThinking.Length <= 180 ? fullThinking : fullThinking[..180].TrimEnd() + "…";
        return CollapsedThinkingPrefix + preview;
    }

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

    /// <summary>Добавить сообщение из внешнего MCP (<c>send_chat</c> с <c>role=assistant</c>).</summary>
        /// <summary>Добавить сообщение из внешнего MCP (<c>send_chat</c> с <c>role=assistant</c>).
    /// Dual-cockpit voice: never drop the bubble if bracket prepare fails — keep prose `[F:…]` for click-time resolve.</summary>
    public async Task<string> AppendMessageFromMcpAsync(string role, string content, CancellationToken cancellationToken = default)
    {
        var r = string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase) ? "assistant" : "user";
        var trimmed = content.Trim();
        if (trimmed.Length == 0)
            return "Empty content";

        var body = trimmed;
        IReadOnlyList<AttachmentAnchor> attachments = [];
        SenderWorkspaceContext? senderContext = null;
        string? statusHint = null;
        if (IntercomAttachSyntax.HasWireOrBracketSyntax(trimmed))
        {
            var (editor, workspace, solution, pending) = await UiScheduler.Default.InvokeAsync(() =>
            {
                var p = new Dictionary<string, AttachmentAnchor>(_pendingAttachDrafts, StringComparer.OrdinalIgnoreCase);
                return (
                    IntercomAttachmentResolveAtSend.EditorSnapshot.ForMcpBracketResolve(_getCurrentFilePath?.Invoke()),
                    ResolveAttachWorkspaceRoot(),
                    ResolveAttachSolutionPath(),
                    p);
            }).ConfigureAwait(false);

            var prepared = await IntercomSendTrace.RunAsync(
                workspace,
                "AppendMessageFromMcp.Prepare",
                async phase =>
                {
                    var result = await IntercomOutboundMessagePreparer.PrepareForMcpAsync(
                        trimmed,
                        pending,
                        editor,
                        workspace,
                        solution,
                        cancellationToken).ConfigureAwait(false);
                    phase.Detail(result.Status.ToString());
                    return result;
                }).ConfigureAwait(false);

            if (prepared.IsCommittable)
            {
                body = prepared.Outbound.Content;
                attachments = prepared.Outbound.Attachments;
                senderContext = prepared.Outbound.SenderWorkspaceContext;
                statusHint = IntercomPreparedMessageCommit.FormatStatusHint(prepared);
            }
            else
            {
                // Keep original prose (incl. `[F:…]`) so the bubble still lands; reveal resolves on click.
                var err = prepared.Error ?? "Не удалось собрать вложения.";
                statusHint = $"MCP: вложения deferred — {err}";
            }
        }

        // Commit только на UI-потоке; prepare — с ConfigureAwait(false), иначе дедлок с MCP на UI.
        return await UiScheduler.Default.InvokeAsync(() =>
        {
            if (!string.IsNullOrWhiteSpace(statusHint))
                ClarificationStatusText = statusHint;

            var vm = new ChatMessageViewModel(
                r,
                body,
                threadId: _activeThreadId,
                attachments: attachments,
                senderWorkspaceContext: senderContext);
            ChatMessages.Add(vm);
            _ = PersistEventAsync(ChatHistoryEventKind.MessageAdded, ChatHistoryPayloadMapping.ToMessagePayload(vm));
            if (string.Equals(r, "assistant", StringComparison.Ordinal))
                _ = PersistEventAsync(ChatHistoryEventKind.MessageCompleted, ChatHistoryPayloadMapping.ToMessagePayload(vm));
            return "OK";
        }).ConfigureAwait(false);
    }
}
