#nullable enable
using System.Collections;
using System.ComponentModel;
using System.Text;
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
internal static partial class CascadeIdeMafIdeAgentChat
{
    internal const int SalvageOutcomeMaxCharsForSummary = 18_000;
    internal const int MafDynamicPromptPacksMaxChars = 2_200;

    /// <summary>
    /// Макс. длина одного сообщения с ролью <c>tool</c> при сборке истории для MAF/Ollama (малый контекст — не забиваем окно длинными трассами UI).
    /// </summary>
    internal const int MafHistoryToolBubbleMaxChars = 960;

    /// <inheritdoc cref="RunAsync(IChatClient, IReadOnlyList{ChatMessage}, string?, string?, Func{string, IReadOnlyDictionary{string, JsonElement}?, CancellationToken, Task{string}}, CancellationToken)" />
    internal static Task<(string AssistantText, IReadOnlyList<string> ToolUiBubbles, Fm.FmTurnUsage? Usage)> RunAsync(
        Uri ollamaBaseUri,
        string modelId,
        IReadOnlyList<ChatMessage> cascadeConversation,
        string? minimizedContextBlock,
        string? projectAgentRulesMarkdown,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> executeIdeCommandAsync,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ollamaBaseUri);
        if (string.IsNullOrWhiteSpace(modelId))
            throw new ArgumentException("Model id is required.", nameof(modelId));

        Microsoft.Extensions.AI.IChatClient client = new OllamaChatClient(ollamaBaseUri, modelId.Trim());
        return RunAsync(client, cascadeConversation, minimizedContextBlock, projectAgentRulesMarkdown, executeIdeCommandAsync, cancellationToken);
    }

    /// <summary>
    /// Запуск агента MAF. <paramref name="ToolUiBubbles"/> — по одному элементу на шаг инструмента для панели чата
    /// (отдельные пузыри «Инструмент»); при совпадении числа с <see cref="FunctionCallContent"/> в ответе сверху добавляется блок параметров.
    /// </summary>
    public static async Task<(string AssistantText, IReadOnlyList<string> ToolUiBubbles, Fm.FmTurnUsage? Usage)> RunAsync(
        Microsoft.Extensions.AI.IChatClient chatClient,
        IReadOnlyList<ChatMessage> cascadeConversation,
        string? minimizedContextBlock,
        string? projectAgentRulesMarkdown,
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

        var prompts = MafIdeAgentPrompts.Current;

        AIAgent agent = chatClient.AsAIAgent(
            instructions: BuildInstructions(prompts, cascadeConversation, minimizedContextBlock, projectAgentRulesMarkdown),
            tools: BuildMafToolList(executeIdeCommandAsync, toolTraces, includeCatalogDebugExtras));

        var messages = BuildMeAiMessages(cascadeConversation, minimizedContextBlock);
        if (messages.Count == 0)
            return ("Нет сообщений для модели.", [], null);

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken).ConfigureAwait(false);
        var assistantText = ExtractAssistantText(response);

        // Локальные модели часто печатают «вызов тула» как JSON в тексте вместо нативных tool_calls —
        // FunctionInvokingChatClient тогда не исполняет ничего; вытаскиваем известные intent и прокидываем в тот же exec.
        if (toolTraces.Count == 0)
        {
            var salvaged = await TrySalvageAssistantTextAsToolCallAsync(
                    assistantText,
                    executeIdeCommandAsync,
                    toolTraces,
                    includeCatalogDebugExtras,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrEmpty(salvaged))
            {
                assistantText = await SummarizeSalvagedToolOutcomeAsync(
                        chatClient,
                        salvaged,
                        prompts,
                        cascadeConversation,
                        toolTraces,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        var uiBubbles = BuildToolUiBubbles(response, toolTraces);
        var usage = Fm.FmMeAiUsageExtractor.TryFromAgentResponse(response);
        return (assistantText, uiBubbles, usage);
    }

    internal static string BuildInstructions(
        MafIdeAgentPrompts.PromptPack prompts,
        IReadOnlyList<ChatMessage> cascadeConversation,
        string? minimizedContextBlock,
        string? projectAgentRulesMarkdown)
    {
        var core = prompts.AgentSystem.Trim();
        var sb = new StringBuilder(core);

        var route = MafPromptPackRouter.Route(prompts, cascadeConversation, minimizedContextBlock, MafDynamicPromptPacksMaxChars);
        if (route.Selections.Count > 0)
        {
            sb.Append("\n\n---\n\n## Routing Abstraction State\n\n");
            sb.Append(MafPromptPackRouter.FormatRoutingStateForPrompt(route.State));
            sb.Append("\n\n---\n\n## Dynamic Prompt Packs\n\n");
            foreach (var selection in route.Selections)
            {
                sb.Append("### ").Append(selection.Key).Append('\n');
                sb.Append("_score=").Append(selection.Score).Append("; reasons=");
                sb.Append(selection.Reasons.Count > 0 ? string.Join(", ", selection.Reasons) : "n/a");
                sb.Append("_\n\n");
                sb.Append(selection.Text).Append('\n').Append('\n');
            }
        }

        var extra = projectAgentRulesMarkdown?.Trim();
        if (!string.IsNullOrEmpty(extra))
            sb.Append("\n\n---\n\n## Проектные правила (workspace)\n\n").Append(extra);

        return sb.ToString().TrimEnd();
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
                    toolTraces[^1] = IdeMcpToolResultPlainFormatter.ForUiTrace(tool.Name, outcome);
                    return IdeMcpToolResultPlainFormatter.ForModel(tool.Name, outcome);
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
                    toolTraces[^1] = IdeMcpToolResultPlainFormatter.ForUiTrace(command_id, outcome);
                    return IdeMcpToolResultPlainFormatter.ForModel(command_id, outcome);
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

    private static List<MeAiChat> BuildMeAiMessages(IReadOnlyList<ChatMessage> cascadeConversation, string? minimizedContextBlock)
    {
        var list = new List<MeAiChat>();
        var context = minimizedContextBlock?.Trim();
        if (!string.IsNullOrEmpty(context))
            list.Add(new MeAiChat(MeAiRole.User, "Контекст текущего файла (только диагностики и сигнатуры):\n\n" + context));

        foreach (var m in cascadeConversation)
        {
            if (string.Equals(m.Role, "tool", StringComparison.OrdinalIgnoreCase))
            {
                var toolText = ClampForMafHistory(m.Content ?? "", MafHistoryToolBubbleMaxChars);
                if (toolText.Length > 0)
                    list.Add(new MeAiChat(MeAiRole.Tool, toolText));
                continue;
            }

            if (!TryMapRole(m.Role, out var r))
                continue;
            list.Add(new MeAiChat(r, m.Content ?? ""));
        }

        return list;
    }

    private static string ClampForMafHistory(string text, int maxChars)
    {
        text = text.Trim();
        if (text.Length == 0)
            return "";
        if (text.Length <= maxChars)
            return text;
        return text[..maxChars].TrimEnd() + "\n… [усечено для контекста]";
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

    private static string? GetLastCascadeUserMessagePlain(IReadOnlyList<ChatMessage> cascadeConversation)
    {
        for (var i = cascadeConversation.Count - 1; i >= 0; i--)
        {
            var m = cascadeConversation[i];
            if (!string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase))
                continue;
            var t = (m.Content ?? "").Trim();
            if (t.Length > 0)
                return t;
        }

        return null;
    }
}
