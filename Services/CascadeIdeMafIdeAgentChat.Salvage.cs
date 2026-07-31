#nullable enable
using System.Collections;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using MeAiChat = Microsoft.Extensions.AI.ChatMessage;
using MeAiRole = Microsoft.Extensions.AI.ChatRole;

namespace CascadeIDE.Services;

internal static partial class CascadeIdeMafIdeAgentChat
{
    /// <summary>Один вызов базового клиента без тулов и без Agent Framework — иначе Ollama снова может выдать JSON вместо пересказа.</summary>
    private static async Task<string> SummarizeSalvagedToolOutcomeAsync(
        IChatClient chatClient,
        string toolOutcome,
        MafIdeAgentPrompts.PromptPack prompts,
        IReadOnlyList<ChatMessage> cascadeConversation,
        List<string> toolTraces,
        CancellationToken ct)
    {
        if (toolOutcome.Length == 0)
            return toolOutcome;

        string payload = IdeMcpToolResultPlainFormatter.ForSalvagePayload(toolOutcome, SalvageOutcomeMaxCharsForSummary);

        string userQuery =
            GetLastCascadeUserMessagePlain(cascadeConversation) ?? "(нет текста последнего сообщения пользователя)";

        var userBlock = prompts.BuildSalvageUserMessage(userQuery, payload);

        var briefChat = new List<MeAiChat>
        {
            new(MeAiRole.System, prompts.SalvageRecapSystem),
            new(MeAiRole.User, userBlock),
        };

        toolTraces.Add("[salvage:пересказ] запрос модели без тулов…");
        try
        {
            ChatResponse recap = await chatClient.GetResponseAsync(briefChat, cancellationToken: ct).ConfigureAwait(false);
            var recapText = (recap.Text ?? "").Trim();
            if (recapText.Length == 0)
            {
                toolTraces[^1] = "[salvage:пересказ] пустой ответ — оставлен сырой результат тула.";
                return toolOutcome;
            }

            toolTraces[^1] = "[salvage:пересказ] ок";
            return recapText;
        }
        catch (OperationCanceledException)
        {
            toolTraces[^1] = "[salvage:пересказ] отмена.";
            throw;
        }
        catch (Exception ex)
        {
            toolTraces[^1] = $"[salvage:пересказ] ошибка: {ex.Message} — оставлен сырой результат.";
            return toolOutcome;
        }
    }

    private static async Task<string?> TrySalvageAssistantTextAsToolCallAsync(
        string assistantText,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> executeIdeCommandAsync,
        List<string> toolTraces,
        bool includeCatalogDebugExtras,
        CancellationToken cancellationToken)
    {
        var unwrap = UnwrapMarkdownJsonFence(assistantText);
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(unwrap);
        }
        catch (JsonException)
        {
            return null;
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return null;
            if (!root.TryGetProperty("name", out var nameProp) || nameProp.ValueKind != JsonValueKind.String)
                return null;

            string toolName = nameProp.GetString() ?? "";
            if (toolName.Length == 0)
                return null;

            string argumentsJson = root.TryGetProperty("arguments", out var argsProp)
                ? argsProp.GetRawText()
                : "{}";

            var promoted = CascadeIdeMafPromotedTools.BuildLookup(includeCatalogDebugExtras);

            if (string.Equals(toolName, "execute_ide_command", StringComparison.Ordinal))
            {
                return await InvokeSalvagedExecuteIdeCommandAsync(
                        argumentsJson,
                        executeIdeCommandAsync,
                        toolTraces,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (!promoted.Contains(toolName))
                return null;

            return await InvokeSalvagedPromotedToolAsync(
                    toolName,
                    argumentsJson,
                    executeIdeCommandAsync,
                    toolTraces,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static string UnwrapMarkdownJsonFence(string text)
    {
        var t = text.Trim();
        if (t.Length == 0 || !t.StartsWith("```", StringComparison.Ordinal))
            return t;

        var firstNl = t.IndexOf('\n');
        if (firstNl < 0)
            return t;

        var close = t.LastIndexOf("```", StringComparison.Ordinal);
        if (close <= firstNl)
            return t;

        return t.AsSpan(firstNl + 1, close - firstNl - 1).Trim().ToString();
    }

    private static async Task<string?> InvokeSalvagedPromotedToolAsync(
        string toolName,
        string argumentsJson,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> exec,
        List<string> toolTraces,
        CancellationToken ct)
    {
        if (!CascadeIdeMafPromotedTools.TryMcpProxyToolToCommandId(toolName, out var commandId))
            return null;

        using JsonDocument argsDoc = JsonDocument.Parse(argumentsJson);
        JsonElement arguments = argsDoc.RootElement;

        string traceHeader = $"[{toolName}]";
        toolTraces.Add($"{traceHeader} salvage:text-json вызов…");
        try
        {
            var argsDict = CascadeIdeMafPromotedTools.JsonArgsToDict(arguments);
            var outcome = await exec(commandId, argsDict, ct).ConfigureAwait(false);
            toolTraces[^1] = IdeMcpToolResultPlainFormatter.ForUiTrace(toolName + " (salvage)", outcome);
            return outcome;
        }
        catch (OperationCanceledException)
        {
            toolTraces[^1] = $"{traceHeader} salvage → отмена";
            throw;
        }
        catch (Exception ex)
        {
            toolTraces[^1] = $"{traceHeader} salvage → ошибка: {ex.Message}";
            return $"[{toolName}] ошибка (salvage): {ex.Message}";
        }
    }

    private static async Task<string?> InvokeSalvagedExecuteIdeCommandAsync(
        string argumentsJson,
        Func<string, IReadOnlyDictionary<string, JsonElement>?, CancellationToken, Task<string>> exec,
        List<string> toolTraces,
        CancellationToken ct)
    {
        using JsonDocument argsDoc = JsonDocument.Parse(argumentsJson);
        JsonElement arguments = argsDoc.RootElement;
        if (arguments.ValueKind != JsonValueKind.Object)
            return null;
        if (!arguments.TryGetProperty("command_id", out var cid) || cid.ValueKind != JsonValueKind.String)
            return null;

        string commandId = (cid.GetString() ?? "").Trim();
        if (commandId.Length == 0)
            return null;

        string? argsJson = null;
        if (arguments.TryGetProperty("args_json", out var aj))
        {
            argsJson = aj.ValueKind switch
            {
                JsonValueKind.String => aj.GetString(),
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => aj.GetRawText(),
            };
        }

        string traceHeader = $"[{commandId}]";
        toolTraces.Add($"{traceHeader} salvage:text-json вызов…");
        try
        {
            var args = IdeCommandRegistry.ParseArgs(string.IsNullOrWhiteSpace(argsJson) ? null : argsJson.Trim());
            var outcome = await exec(commandId, args, ct).ConfigureAwait(false);
            toolTraces[^1] = IdeMcpToolResultPlainFormatter.ForUiTrace(commandId + " (salvage)", outcome);
            return outcome;
        }
        catch (OperationCanceledException)
        {
            toolTraces[^1] = $"{traceHeader} salvage → отмена";
            throw;
        }
        catch (Exception ex)
        {
            toolTraces[^1] = $"{traceHeader} salvage → ошибка: {ex.Message}";
            return $"[execute_ide_command] ошибка (salvage): {ex.Message}";
        }
    }

    /// <summary>Один элемент — один пузырь «Инструмент» в UI; при возможности дополняет трассу параметрами из <see cref="FunctionCallContent"/>.</summary>
    private static IReadOnlyList<string> BuildToolUiBubbles(AgentResponse response, List<string> toolTraces)
    {
        if (toolTraces.Count == 0)
            return [];

        var calls = ExtractOrderedFunctionCalls(response);
        var hasSalvageOrRecap = toolTraces.Exists(static t =>
            t.Contains("salvage", StringComparison.OrdinalIgnoreCase));

        if (!hasSalvageOrRecap && calls.Count == toolTraces.Count)
        {
            var merged = new List<string>(toolTraces.Count);
            for (var i = 0; i < toolTraces.Count; i++)
            {
                var argsBlock = FormatArgsBlockForUi(calls[i].Name, calls[i].ArgsJson);
                merged.Add(string.IsNullOrEmpty(argsBlock) ? toolTraces[i] : $"{argsBlock}\n\n{toolTraces[i]}");
            }

            return merged;
        }

        return [.. toolTraces];
    }

    private static string FormatArgsBlockForUi(string toolName, string argsJson)
    {
        var trimmed = argsJson.Trim();
        if (trimmed is "" or "{}")
            return "";

        const int max = 1200;
        if (trimmed.Length > max)
            trimmed = trimmed[..max] + "\n…";

        return $"Параметры `{toolName}`:\n{trimmed}";
    }

    private static List<(string Name, string ArgsJson)> ExtractOrderedFunctionCalls(AgentResponse response)
    {
        var list = new List<(string Name, string ArgsJson)>();
        if (response.Messages is not { Count: > 0 })
            return list;

        foreach (var m in response.Messages)
        {
            foreach (var c in m.Contents)
            {
                if (c is FunctionCallContent { InformationalOnly: false } fcc)
                {
                    var argsJson = SerializeArgumentsForUi((object?)fcc.Arguments);
                    list.Add((fcc.Name, argsJson));
                }
            }
        }

        return list;
    }

    private static string SerializeArgumentsForUi(object? arguments)
    {
        if (arguments is null)
            return "{}";

        try
        {
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                switch (arguments)
                {
                    case IDictionary<string, object?> d:
                        foreach (var kv in d)
                        {
                            writer.WritePropertyName(kv.Key);
                            WriteJsonValue(writer, kv.Value);
                        }

                        break;
                    case IDictionary legacy:
                        foreach (DictionaryEntry e in legacy)
                        {
                            writer.WritePropertyName(e.Key?.ToString() ?? "");
                            WriteJsonValue(writer, e.Value);
                        }

                        break;
                    default:
                        writer.WritePropertyName("_");
                        WriteJsonValue(writer, arguments);
                        break;
                }

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch
        {
            return "{}";
        }
    }

    private static void WriteJsonValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case byte n:
                writer.WriteNumberValue(n);
                break;
            case sbyte n:
                writer.WriteNumberValue(n);
                break;
            case short n:
                writer.WriteNumberValue(n);
                break;
            case ushort n:
                writer.WriteNumberValue(n);
                break;
            case int n:
                writer.WriteNumberValue(n);
                break;
            case uint n:
                writer.WriteNumberValue(n);
                break;
            case long n:
                writer.WriteNumberValue(n);
                break;
            case ulong n:
                writer.WriteNumberValue(n);
                break;
            case float n:
                writer.WriteNumberValue(n);
                break;
            case double n:
                writer.WriteNumberValue(n);
                break;
            case decimal n:
                writer.WriteNumberValue(n);
                break;
            case JsonElement je:
                je.WriteTo(writer);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
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
