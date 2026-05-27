#nullable enable
using System.Text.Json;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Services;

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
public sealed class ChatSlashCommandRunner
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
        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp)
        {
            var helpText = string.IsNullOrWhiteSpace(argsTail)
                ? IntercomHelpGuide.FormatFull()
                : string.Join(Environment.NewLine, ChatSlashCommandCatalog.ListHelpLines(argsTail));
            return new ChatSlashCommandRunResult(
                true,
                true,
                displayPath,
                argsTail,
                helpText);
        }

        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalReport)
        {
            var snapshot = _getChatSurfaceSnapshot?.Invoke() ?? ChatSurfaceSnapshot.Empty;
            var report = ChatSlashSessionReports.TryFormat(descriptor.SlashPath, snapshot)
                ?? "Отчёт недоступен.";
            return new ChatSlashCommandRunResult(true, true, displayPath, argsTail, report);
        }

        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalIntercom)
        {
            if (_selectChatThread is null || _setChatOverviewMode is null)
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Intercom navigation недоступна.");
            }

            var snapshot = _getChatSurfaceSnapshot?.Invoke() ?? ChatSurfaceSnapshot.Empty;
            var selectedId = _getSelectedChatThreadId?.Invoke() ?? Guid.Empty;
            if (!ChatSlashIntercomActions.TryExecute(
                    descriptor.SlashPath,
                    argsTail,
                    selectedId,
                    _selectChatThread,
                    _setChatOverviewMode,
                    snapshot,
                    out var intercom,
                    _setTopicPicker,
                    _createTopicWithTitle,
                    _renameTopicWithTitle,
                    _tryAttachSlash,
                    _selectMessageByOrdinalRangeInDetailLane,
                    _selectMessagesByOrdinalRangesInDetailLane,
                    _clearMessageSelectionInDetailLane,
                    _findMessagesForCodeRef,
                    _relateMessageRangeToCodeRef,
                    _listMessageAnchors,
                    _peekAnchorById,
                    _runIntercomAdmin))
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Действие недоступно.");
            }

            return new ChatSlashCommandRunResult(
                true,
                intercom.Success,
                displayPath,
                argsTail,
                intercom.Message);
        }

        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalAgent)
        {
            if (_agentEnvironment is null || _getSolutionPathForAgent is null)
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Agent Execution Environment недоступен.");
            }

            if (!ChatSlashAgentActions.TryExecute(
                    descriptor.SlashPath,
                    argsTail,
                    _agentEnvironment,
                    _getSolutionPathForAgent,
                    out var agent))
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Действие agent недоступно.");
            }

            return new ChatSlashCommandRunResult(
                true,
                agent.Success,
                displayPath,
                argsTail,
                agent.Message);
        }

        var validationError = ValidateRequiredArgs(descriptor, argsTail);
        if (validationError is not null)
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, validationError);

        if (_executeIdeCommand is null)
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "IDE command bridge недоступен для слэш-команд.");
        }

        IReadOnlyDictionary<string, JsonElement>? args;
        if (ChatSlashParametricArgsBuilder.IsParametricCatalogCommand(descriptor.CommandId))
        {
            if (_getEditorContext is null)
            {
                return new ChatSlashCommandRunResult(
                    true,
                    false,
                    displayPath,
                    argsTail,
                    "Контекст редактора недоступен для параметрической слэш-команды.");
            }

            if (!ChatSlashParametricArgsBuilder.TryBuild(
                    descriptor.CommandId,
                    argsTail ?? "",
                    _getEditorContext(),
                    out args,
                    out var parametricError))
            {
                return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, parametricError);
            }
        }
        else if (!TryBuildPathArgs(descriptor, argsTail, out args, out var pathError))
        {
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, pathError);
        }
        else if (args is null)
        {
            args = BuildArgs(descriptor, argsTail);
        }

        try
        {
            var json = await _executeIdeCommand(descriptor.CommandId, args, cancellationToken).ConfigureAwait(false);
            return new ChatSlashCommandRunResult(
                true,
                true,
                displayPath,
                argsTail,
                FormatSuccessDetail(json));
        }
        catch (Exception ex)
        {
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, ex.Message);
        }
    }

    private bool TryBuildPathArgs(
        ChatSlashCommandDescriptor descriptor,
        string? argTail,
        out IReadOnlyDictionary<string, JsonElement>? args,
        out string? error)
    {
        args = null;
        error = null;

        if (descriptor.CommandId is not (IdeCommands.OpenFile or IdeCommands.LoadSolution))
            return true;

        var pathArg = argTail;
        var workspaceRoot = _getWorkspaceRoot?.Invoke();
        if (!ChatSlashWorkspacePathHelper.TryNormalizePathArgument(
                pathArg,
                workspaceRoot,
                out var fullPath,
                out error))
        {
            return false;
        }

        if (descriptor.CommandId == IdeCommands.OpenFile && !File.Exists(fullPath!))
        {
            error = "Файл не найден: " + fullPath;
            return false;
        }

        if (descriptor.CommandId == IdeCommands.LoadSolution
            && !File.Exists(fullPath!)
            && !Directory.Exists(fullPath!))
        {
            error = "Путь не найден: " + fullPath;
            return false;
        }

        args = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["path"] = JsonSerializer.SerializeToElement(fullPath),
        };
        return true;
    }

    private static IReadOnlyDictionary<string, JsonElement>? BuildArgs(
        ChatSlashCommandDescriptor descriptor,
        string? argTail)
    {
        if (!string.IsNullOrEmpty(descriptor.MfdPage))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["page"] = JsonSerializer.SerializeToElement(descriptor.MfdPage),
            };
        }

        if (!string.IsNullOrEmpty(descriptor.PrimarySurface))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["surface"] = JsonSerializer.SerializeToElement(descriptor.PrimarySurface),
            };
        }

        if (!string.IsNullOrEmpty(descriptor.MapLevel))
        {
            return new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["level"] = JsonSerializer.SerializeToElement(descriptor.MapLevel),
            };
        }

        if (string.IsNullOrWhiteSpace(argTail))
            return null;

        return descriptor.CommandId switch
        {
            IdeCommands.ChatSetProductSpine => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["current_focus"] = JsonSerializer.SerializeToElement(argTail),
            },
            IdeCommands.ChatExportReadable => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["write_file"] = JsonSerializer.SerializeToElement(false),
            },
            IdeCommands.GitCommit => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["message"] = JsonSerializer.SerializeToElement(ChatSlashArgsTail.NormalizeFreeText(argTail)),
            },
            IdeCommands.GitDiff when !string.IsNullOrWhiteSpace(argTail) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["path"] = JsonSerializer.SerializeToElement(argTail.Trim()),
                },
            IdeCommands.GitLog when int.TryParse(argTail.Trim(), out var n) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["n"] = JsonSerializer.SerializeToElement(n),
                },
            IdeCommands.SearchWorkspaceText when !string.IsNullOrWhiteSpace(argTail) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["pattern"] = JsonSerializer.SerializeToElement(argTail.Trim()),
                },
            IdeCommands.CreateProjectInSolution
                when TryGetSolutionNewTemplate(descriptor.SlashPath, out var template) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["template"] = JsonSerializer.SerializeToElement(template),
                    ["project_name"] = JsonSerializer.SerializeToElement(argTail.Trim()),
                },
            _ => null,
        };
    }

    private static bool TryGetSolutionNewTemplate(string slashPath, out string template)
    {
        const string prefix = "/solution new ";
        if (slashPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            template = slashPath[prefix.Length..].Trim().ToLowerInvariant();
            if (template is "console" or "classlib" or "webapi")
                return true;
        }

        template = "";
        return false;
    }

    private static string? ValidateRequiredArgs(
        ChatSlashCommandDescriptor descriptor,
        string? argTail)
    {

        if (descriptor.CommandId == IdeCommands.GitCommit && string.IsNullOrWhiteSpace(argTail))
            return "Укажи сообщение коммита: /git commit <message>";

        if (descriptor.CommandId == IdeCommands.SearchWorkspaceText && string.IsNullOrWhiteSpace(argTail))
            return "Укажи шаблон поиска: /search <pattern>";

        if (descriptor.CommandId is IdeCommands.OpenFile or IdeCommands.LoadSolution
            && string.IsNullOrWhiteSpace(argTail))
        {
            return descriptor.CommandId == IdeCommands.OpenFile
                ? "Укажи путь к файлу: /file open <path>"
                : "Укажи путь: /solution load <path>";
        }

        if (descriptor.CommandId == IdeCommands.CreateProjectInSolution)
        {
            if (!TryGetSolutionNewTemplate(descriptor.SlashPath, out var template))
                return "Укажи шаблон: /solution new console|classlib|webapi <имя>";
            if (string.IsNullOrWhiteSpace(argTail))
                return $"Укажи имя проекта: /solution new {template} <имя>";
        }

        if (IntentMelodyCatalog.TryGetParametricRootByCommandId(descriptor.CommandId, out var parametricRoot)
            && ChatSlashParametricArgsBuilder.RequiresNonEmptyArgsTail(parametricRoot)
            && string.IsNullOrWhiteSpace(argTail))
        {
            return parametricRoot.WireClass switch
            {
                "int_chain_colon_space" =>
                    "Укажи строки (1-based): одну, «5 10» или «5:10».",
                _ => "Укажи параметры команды в хвосте строки.",
            };
        }

        return null;
    }

    /// <summary>Текст под командой только если есть что показать; «ok» / <c>{"ok":true}</c> не выводим.</summary>
    private static string? FormatSuccessDetail(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var trimmed = json.Trim();
        if (trimmed.Length == 0 || IsTrivialAck(trimmed))
            return null;

        return trimmed.Length <= 400 ? trimmed : $"Ответ ({trimmed.Length} символов).";
    }

    private static bool IsTrivialAck(string trimmed)
    {
        if (string.Equals(trimmed, "ok", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!trimmed.StartsWith('{'))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            if (!doc.RootElement.TryGetProperty("ok", out var ok))
                return false;
            if (ok.ValueKind is JsonValueKind.True)
                return doc.RootElement.GetPropertyCount() <= 2;
            if (ok.ValueKind is JsonValueKind.String
                && string.Equals(ok.GetString(), "true", StringComparison.OrdinalIgnoreCase))
                return doc.RootElement.GetPropertyCount() <= 2;
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }
}
