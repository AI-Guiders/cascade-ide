#nullable enable
using System.Collections.ObjectModel;
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
}
