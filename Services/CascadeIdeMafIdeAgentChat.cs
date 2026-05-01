#nullable enable
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Protocol;
using MeAiChat = Microsoft.Extensions.AI.ChatMessage;
using MeAiRole = Microsoft.Extensions.AI.ChatRole;

namespace CascadeIDE.Services;

/// <summary>
/// Чат IDE через Microsoft Agent Framework + любой <see cref="IChatClient"/>: те же IDE-команды, что MCP (<see cref="IIdeMcpActions.ExecuteCommandAsync"/>).
/// Локально — Ollama; в облаке — Anthropic / OpenAI-совместимые (OpenAI, DeepSeek) через официальные клиенты.
/// </summary>
internal static class CascadeIdeMafIdeAgentChat
{
    private const string SystemInstructionsRu =
        "Ты ассистент внутри Cascade IDE. Частые действия (открыть файл, состояние IDE, сборка, тесты, поиск по репозиторию, брейкпоинты) "
        + "вызывай именованными инструментами с теми же именами, что у MCP: ide_open_file, ide_load_solution, ide_get_ide_state, ide_build, ide_run_tests, "
        + "ide_search_workspace_text, ide_set_breakpoint и др. Передавай аргументы как JSON-поля тула по схеме MCP."
        + " Для любой другой команды IDE используй execute_ide_command: command_id как в docs/MCP-PROTOCOL.md "
        + "без префикса ide_ (например open_file, build_structured); args_json — JSON-объект аргументов или пустая строка.";

    /// <inheritdoc cref="RunAsync(IChatClient, IReadOnlyList{ChatMessage}, string?, Func{string, IReadOnlyDictionary{string, JsonElement}?, CancellationToken, Task{string}}, CancellationToken)" />
    internal static Task<(string AssistantText, IReadOnlyList<string> ToolTraces)> RunAsync(
        Uri ollamaBaseUri,
        string modelId,
        IReadOnlyList<ChatMessage> cascadeConversation,
        string? minimizedContextBlock,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> executeIdeCommandAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ollamaBaseUri);
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model id is required.", nameof(modelId));

        Microsoft.Extensions.AI.IChatClient client = new OllamaChatClient(ollamaBaseUri, modelId.Trim());
        return RunAsync(client, cascadeConversation, minimizedContextBlock, executeIdeCommandAsync, cancellationToken);
    }

    public static async Task<(string AssistantText, IReadOnlyList<string> ToolTraces)> RunAsync(
        Microsoft.Extensions.AI.IChatClient chatClient,
        IReadOnlyList<ChatMessage> cascadeConversation,
        string? minimizedContextBlock,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> executeIdeCommandAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(cascadeConversation);
        ArgumentNullException.ThrowIfNull(executeIdeCommandAsync);

        var toolTraces = new List<string>();

        bool includeCatalogDebugExtras =
#if DEBUG
            true;
#else
            false;
#endif

        AIAgent agent = chatClient.AsAIAgent(
            instructions: SystemInstructionsRu,
            tools: BuildMafToolList(executeIdeCommandAsync, toolTraces, includeCatalogDebugExtras));

        var messages = BuildMeAiMessages(cascadeConversation, minimizedContextBlock);
        if (messages.Count == 0)
            return ("Нет сообщений для модели.", toolTraces);

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken).ConfigureAwait(false);
        var assistantText = ExtractAssistantText(response);
        return (assistantText, toolTraces);
    }

    private static List<AITool> BuildMafToolList(
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> exec,
        List<string> toolTraces,
        bool includeCatalogDebugExtras)
    {
        var promotedLookup = CascadeIdeMafPromotedTools.BuildLookup(includeCatalogDebugExtras);
        var catalog = IdeMcpToolCatalog.BuildTools(includeCatalogDebugExtras);
        var list = new List<AITool>();
        foreach (var tool in catalog.OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            if (!promotedLookup.Contains(tool.Name))
                continue;

            // Диспетчер MCP остаётся только через execute_ide_command (единый текстовый command_id).
            if (string.Equals(tool.Name, "ide_execute_command", StringComparison.Ordinal))
                continue;

            if (!CascadeIdeMafPromotedTools.TryMcpProxyToolToCommandId(tool.Name, out _))
                continue;

            list.Add(CreatePromotedCatalogTool(tool, exec, toolTraces));
        }

        list.Add(CreateIdeDispatchTool(exec, toolTraces));
        return list;
    }

    private static AIFunction CreatePromotedCatalogTool(
        Tool tool,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> exec,
        List<string> toolTraces)
    {
        if (!CascadeIdeMafPromotedTools.TryMcpProxyToolToCommandId(tool.Name, out var commandId))
            throw new InvalidOperationException("Unexpected promoted tool without ide_ proxy mapping: " + tool.Name);

        var descriptionForModel = ClampDescription(tool.Description, maxChars: 3600);

        return AIFunctionFactory.Create(
            async (
                [Description("Объект аргументов: поля как у MCP-схемы этого тула (см. описание ниже или docs/MCP-PROTOCOL.md). Для тула без аргументов — пустой объект {}.")]
                JsonElement arguments,
                CancellationToken cancellationToken) =>
            {
                string traceHeader = $"[{tool.Name}]";
                toolTraces.Add($"{traceHeader} вызов…");

                try
                {
                    var argsDict = CascadeIdeMafPromotedTools.JsonArgsToDict(arguments);
                    var outcome = await exec(commandId, argsDict, cancellationToken).ConfigureAwait(false);
                    toolTraces[^1] = $"{traceHeader} → {SummarizeOutcome(outcome)}";
                    return outcome;
                }
                catch (OperationCanceledException)
                {
                    toolTraces[^1] = $"{traceHeader} → отмена";
                    throw;
                }
                catch (Exception ex)
                {
                    toolTraces[^1] = $"{traceHeader} → ошибка: {ex.Message}";
                    return $"[{tool.Name}] ошибка: {ex.Message}";
                }
            },
            new AIFunctionFactoryOptions
            {
                Name = tool.Name,
                Description = descriptionForModel,
            });
    }

    private static string ClampDescription(string? text, int maxChars)
    {
        var s = (text ?? "").Trim();
        if (s.Length <= maxChars)
            return s.Length > 0 ? s : "(См. схему параметров MCP для этого тула.)";
        return s[..maxChars] + $"… (+{s.Length - maxChars} симв.)";
    }

    private static AIFunction CreateIdeDispatchTool(
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> exec,
        List<string> toolTraces)
    {
        return AIFunctionFactory.Create(
            async (
                [Description("Идентификатор команды IDE (как MCP), напр. open_file, get_editor_state.")]
                string command_id,
                [Description("Необязательные аргументы — строка JSON-объекта, например {\"path\":\"C:\\\\tmp\\\\a.cs\"}.")]
                string? args_json,
                CancellationToken cancellationToken) =>
            {
                command_id = (command_id ?? "").Trim();
                if (command_id.Length == 0)
                    return "[execute_ide_command] ошибка: пустой command_id";

                string traceHeader = $"[{command_id}]";
                toolTraces.Add($"{traceHeader} вызов…");

                try
                {
                    var args = IdeCommandRegistry.ParseArgs(string.IsNullOrWhiteSpace(args_json) ? null : args_json.Trim());
                    var outcome = await exec(command_id, args, cancellationToken).ConfigureAwait(false);
                    toolTraces[^1] = $"{traceHeader} → {SummarizeOutcome(outcome)}";
                    return outcome;
                }
                catch (OperationCanceledException)
                {
                    toolTraces[^1] = $"{traceHeader} → отмена";
                    throw;
                }
                catch (Exception ex)
                {
                    toolTraces[^1] = $"{traceHeader} → ошибка: {ex.Message}";
                    return $"[execute_ide_command] ошибка: {ex.Message}";
                }
            },
            name: "execute_ide_command",
            description:
            "То же что MCP ide_execute_command: command_id + опционально args_json объектом параметров или пусто.");
    }

    private static string SummarizeOutcome(string outcome)
    {
        if (outcome.Length <= 420)
            return outcome;
        return outcome[..380] + "… (" + outcome.Length + " симв.)";
    }

    private static List<MeAiChat> BuildMeAiMessages(IReadOnlyList<ChatMessage> cascadeConversation, string? minimizedContextBlock)
    {
        var list = new List<MeAiChat>();
        var context = minimizedContextBlock?.Trim();
        if (!string.IsNullOrEmpty(context))
            list.Add(new MeAiChat(MeAiRole.User, "Контекст текущего файла (только диагностики и сигнатуры):\n\n" + context));

        foreach (var m in cascadeConversation)
        {
            if (!TryMapRole(m.Role, out var r))
                continue;
            list.Add(new MeAiChat(r, m.Content ?? ""));
        }

        return list;
    }

    private static bool TryMapRole(string role, out MeAiRole r)
    {
        if (string.Equals(role, "user", StringComparison.OrdinalIgnoreCase))
        {
            r = MeAiRole.User;
            return true;
        }

        if (string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase))
        {
            r = MeAiRole.Assistant;
            return true;
        }

        r = MeAiRole.User;
        return false;
    }

    private static string ExtractAssistantText(AgentResponse response)
    {
        try
        {
            if (response.Messages is { Count: > 0 })
            {
                for (var i = response.Messages.Count - 1; i >= 0; i--)
                {
                    var m = response.Messages[i];
                    if (!string.IsNullOrWhiteSpace(m.Text))
                        return m.Text.Trim();

                    foreach (var c in m.Contents)
                    {
                        if (c is TextContent txt && txt.Text.Length > 0)
                            return txt.Text.Trim();
                    }
                }
            }
        }
        catch
        {
            /* fall through */
        }

        return response.ToString().Trim();
    }
}
