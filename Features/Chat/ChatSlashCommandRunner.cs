#nullable enable
using System.Text.Json;
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
    private readonly Func<string, string>? _createTopicWithTitle;

    public ChatSlashCommandRunner(
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>>? executeIdeCommand,
        Func<ChatSlashEditorContext>? getEditorContext = null,
        Func<string>? getWorkspaceRoot = null,
        Func<ChatSurfaceSnapshot>? getChatSurfaceSnapshot = null,
        Func<Guid>? getSelectedChatThreadId = null,
        Action<Guid>? selectChatThread = null,
        Action<bool>? setChatOverviewMode = null,
        Action<TopicPickerPresentation>? setTopicPicker = null,
        Func<string, string>? createTopicWithTitle = null)
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
    }

    public async Task<ChatSlashCommandRunResult> TryRunAsync(string rawInput, CancellationToken cancellationToken = default)
    {
        var parse = ChatSlashCommandParser.TryParse(rawInput);
        if (!parse.IsSlashLine)
            return ChatSlashCommandRunResult.NotHandled();

        var displayPath = ChatSlashCommandPresentation.FormatDisplayPath(parse, rawInput);
        var argsTail = ChatSlashCommandPresentation.NormalizeArgsTail(parse.ArgsTail);

        if (parse.IsRejected)
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                parse.RejectReason ?? "Неизвестная слэш-команда.");

        if (!ChatSlashCommandCatalog.TryResolve(parse, out var descriptor))
        {
            return new ChatSlashCommandRunResult(
                true,
                false,
                displayPath,
                argsTail,
                "Неизвестная команда. Введи /help — список доступных слэш-команд.");
        }

        displayPath = descriptor.SlashPath;
        if (descriptor.ExecutionKind == ChatSlashCommandExecutionKind.LocalHelp)
        {
            return new ChatSlashCommandRunResult(
                true,
                true,
                displayPath,
                argsTail,
                string.Join(Environment.NewLine, ChatSlashCommandCatalog.ListHelpLines(argsTail)));
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
                    _createTopicWithTitle))
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

        var validationError = ValidateRequiredArgs(descriptor, parse);
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
                    parse.ArgsTail,
                    _getEditorContext(),
                    out args,
                    out var parametricError))
            {
                return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, parametricError);
            }
        }
        else if (!TryBuildPathArgs(descriptor, parse, out args, out var pathError))
        {
            return new ChatSlashCommandRunResult(true, false, displayPath, argsTail, pathError);
        }
        else if (args is null)
        {
            args = BuildArgs(descriptor, parse);
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
        ChatSlashCommandParseResult parse,
        out IReadOnlyDictionary<string, JsonElement>? args,
        out string? error)
    {
        args = null;
        error = null;

        if (descriptor.CommandId is not (IdeCommands.OpenFile or IdeCommands.LoadSolution))
            return true;

        var workspaceRoot = _getWorkspaceRoot?.Invoke();
        if (!ChatSlashWorkspacePathHelper.TryNormalizePathArgument(
                parse.ArgsTail,
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
        ChatSlashCommandParseResult parse)
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

        if (string.IsNullOrWhiteSpace(parse.ArgsTail))
            return null;

        return descriptor.CommandId switch
        {
            IdeCommands.ChatSetProductSpine => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["current_focus"] = JsonSerializer.SerializeToElement(parse.ArgsTail),
            },
            IdeCommands.ChatExportReadable => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["write_file"] = JsonSerializer.SerializeToElement(false),
            },
            IdeCommands.GitCommit => new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["message"] = JsonSerializer.SerializeToElement(ChatSlashArgsTail.NormalizeFreeText(parse.ArgsTail)),
            },
            IdeCommands.GitDiff when !string.IsNullOrWhiteSpace(parse.ArgsTail) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["path"] = JsonSerializer.SerializeToElement(parse.ArgsTail.Trim()),
                },
            IdeCommands.GitLog when int.TryParse(parse.ArgsTail.Trim(), out var n) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["n"] = JsonSerializer.SerializeToElement(n),
                },
            IdeCommands.SearchWorkspaceText when !string.IsNullOrWhiteSpace(parse.ArgsTail) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["pattern"] = JsonSerializer.SerializeToElement(parse.ArgsTail.Trim()),
                },
            IdeCommands.CreateProjectInSolution when !string.IsNullOrWhiteSpace(parse.SubAction) =>
                new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    ["template"] = JsonSerializer.SerializeToElement(parse.SubAction.Trim().ToLowerInvariant()),
                    ["project_name"] = JsonSerializer.SerializeToElement(parse.ArgsTail.Trim()),
                },
            _ => null,
        };
    }

    private static string? ValidateRequiredArgs(
        ChatSlashCommandDescriptor descriptor,
        ChatSlashCommandParseResult parse)
    {
        if (descriptor.CommandId == IdeCommands.GitCommit && string.IsNullOrWhiteSpace(parse.ArgsTail))
            return "Укажи сообщение коммита: /git commit <message>";

        if (descriptor.CommandId == IdeCommands.SearchWorkspaceText && string.IsNullOrWhiteSpace(parse.ArgsTail))
            return "Укажи шаблон поиска: /search <pattern>";

        if (descriptor.CommandId is IdeCommands.OpenFile or IdeCommands.LoadSolution
            && string.IsNullOrWhiteSpace(parse.ArgsTail))
        {
            return descriptor.CommandId == IdeCommands.OpenFile
                ? "Укажи путь к файлу: /file open <path>"
                : "Укажи путь: /solution load <path>";
        }

        if (descriptor.CommandId == IdeCommands.CreateProjectInSolution)
        {
            if (string.IsNullOrWhiteSpace(parse.SubAction))
                return "Укажи шаблон: /solution new console|classlib|webapi <имя>";
            if (string.IsNullOrWhiteSpace(parse.ArgsTail))
                return $"Укажи имя проекта: /solution new {parse.SubAction} <имя>";
        }

        if (IntentMelodyCatalog.TryGetParametricRootByCommandId(descriptor.CommandId, out var parametricRoot)
            && ChatSlashParametricArgsBuilder.RequiresNonEmptyArgsTail(parametricRoot)
            && string.IsNullOrWhiteSpace(parse.ArgsTail))
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
