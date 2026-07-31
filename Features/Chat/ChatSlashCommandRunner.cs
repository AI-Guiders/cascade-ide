#nullable enable
using System.Text.Json;
using CascadeIDE.Features.Agent.Environment;

namespace CascadeIDE.Features.Chat;

public sealed record ChatSlashCommandRunResult(
    bool Handled,
    bool Success,
    string SlashPath,
    string? ArgsTail,
    string? DetailText)
{
    public static ChatSlashCommandRunResult NotHandled() => new(false, false, "", null, null);
}

/// <summary>Локальное исполнение слэш-команд до отправки агенту (ADR 0119).</summary>
public sealed partial class ChatSlashCommandRunner
{
    private readonly Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? _executeIdeCommand;
    private readonly Func<ChatSlashEditorContext>? _getEditorContext;
    private readonly Func<string>? _getWorkspaceRoot;
    private readonly Func<ChatSurfaceSnapshot>? _getChatSurfaceSnapshot;
    private readonly Func<Guid>? _getSelectedChatThreadId;
    private readonly Action<Guid>? _selectChatThread;
    private readonly Action<bool>? _setChatOverviewMode;
    private readonly Action<TopicPickerPresentation>? _setTopicPicker;
    private readonly Func<string, TopicCreateResult>? _createTopicWithTitle;
    private readonly Func<Guid, string, TopicRenameResult>? _renameTopicWithTitle;
    private readonly Func<string, string?, ChatSlashIntercomResult>? _tryAttachSlash;
    private readonly Func<int, int, string>? _selectMessageByOrdinalRangeInDetailLane;
    private readonly Func<IReadOnlyList<ParametricIntRange>, string>? _selectMessagesByOrdinalRangesInDetailLane;
    private readonly Func<string>? _clearMessageSelectionInDetailLane;
    private readonly Func<string?, string>? _findMessagesForCodeRef;
    private readonly Func<string?, string>? _relateMessageRangeToCodeRef;
    private readonly Func<string>? _listMessageAnchors;
    private readonly Func<string?, string>? _peekAnchorById;
    private readonly Func<string?>? _getSolutionPathForAgent;
    private readonly IAgentEnvironmentService? _agentEnvironment;
    private Func<string, string?, CancellationToken, Task<ChatSlashIntercomResult>>? _runIntercomAdmin;

    public ChatSlashCommandRunner(
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? executeIdeCommand,
        Func<ChatSlashEditorContext>? getEditorContext = null,
        Func<string>? getWorkspaceRoot = null,
        Func<ChatSurfaceSnapshot>? getChatSurfaceSnapshot = null,
        Func<Guid>? getSelectedChatThreadId = null,
        Action<Guid>? selectChatThread = null,
        Action<bool>? setChatOverviewMode = null,
        Action<TopicPickerPresentation>? setTopicPicker = null,
        Func<string, TopicCreateResult>? createTopicWithTitle = null,
        Func<Guid, string, TopicRenameResult>? renameTopicWithTitle = null,
        Func<string, string?, ChatSlashIntercomResult>? tryAttachSlash = null,
        Func<int, int, string>? selectMessageByOrdinalRangeInDetailLane = null,
        Func<IReadOnlyList<ParametricIntRange>, string>? selectMessagesByOrdinalRangesInDetailLane = null,
        Func<string>? clearMessageSelectionInDetailLane = null,
        Func<string?, string>? findMessagesForCodeRef = null,
        Func<string?, string>? relateMessageRangeToCodeRef = null,
        Func<string>? listMessageAnchors = null,
        Func<string?, string>? peekAnchorById = null,
        IAgentEnvironmentService? agentEnvironment = null,
        Func<string?>? getSolutionPathForAgent = null,
        Func<string, string?, CancellationToken, Task<ChatSlashIntercomResult>>? runIntercomAdmin = null)
    {
        _executeIdeCommand = executeIdeCommand;
        _getEditorContext = getEditorContext;
        _getWorkspaceRoot = getWorkspaceRoot;
        _getChatSurfaceSnapshot = getChatSurfaceSnapshot;
        _getSelectedChatThreadId = getSelectedChatThreadId;
        _selectChatThread = selectChatThread;
        _setChatOverviewMode = setChatOverviewMode;
        _setTopicPicker = setTopicPicker;
        _createTopicWithTitle = createTopicWithTitle;
        _renameTopicWithTitle = renameTopicWithTitle;
        _tryAttachSlash = tryAttachSlash;
        _selectMessageByOrdinalRangeInDetailLane = selectMessageByOrdinalRangeInDetailLane;
        _selectMessagesByOrdinalRangesInDetailLane = selectMessagesByOrdinalRangesInDetailLane;
        _clearMessageSelectionInDetailLane = clearMessageSelectionInDetailLane;
        _findMessagesForCodeRef = findMessagesForCodeRef;
        _relateMessageRangeToCodeRef = relateMessageRangeToCodeRef;
        _listMessageAnchors = listMessageAnchors;
        _peekAnchorById = peekAnchorById;
        _agentEnvironment = agentEnvironment;
        _getSolutionPathForAgent = getSolutionPathForAgent;
        _runIntercomAdmin = runIntercomAdmin;
    }

    public void SetIntercomAdminRunner(
        Func<string, string?, CancellationToken, Task<ChatSlashIntercomResult>> runIntercomAdmin) =>
        _runIntercomAdmin = runIntercomAdmin;

    public async Task<ChatSlashCommandRunResult> TryRunAsync(string rawInput, CancellationToken cancellationToken = default)
    {
        if (!ChatSlashCommandParser.IsSlashLine(rawInput))
            return ChatSlashCommandRunResult.NotHandled();

        var displayPath = ChatSlashCommandPresentation.FormatDisplayPath(rawInput);
        string? argsTail = null;

        if (!ChatSlashCommandCatalog.TryResolveInput(rawInput, out var descriptor, out var resolvedArgTail))
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "Неизвестная команда. Введи /help — список доступных слэш-команд.");
        }

        argsTail = resolvedArgTail;
        displayPath = descriptor.SlashPath;

        return descriptor.ExecutionKind switch
        {
            ChatSlashCommandExecutionKind.LocalHelp =>
                RunLocalHelp(displayPath, argsTail),
            ChatSlashCommandExecutionKind.LocalReport =>
                RunLocalReport(descriptor, displayPath, argsTail),
            ChatSlashCommandExecutionKind.LocalIntercom =>
                RunLocalIntercom(descriptor, displayPath, argsTail),
            ChatSlashCommandExecutionKind.LocalAgent =>
                await RunLocalAgentAsync(descriptor, displayPath, argsTail, cancellationToken).ConfigureAwait(false),
            ChatSlashCommandExecutionKind.ForgeCommand =>
                await RunForgeAsync(descriptor, displayPath, argsTail, cancellationToken).ConfigureAwait(false),
            _ => await RunIdeCommandAsync(descriptor, displayPath, argsTail, cancellationToken).ConfigureAwait(false),
        };
    }
}
