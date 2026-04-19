using System.Text.Json;
using System.Linq;
using ModelContextProtocol.Protocol;

namespace CascadeIDE.Services;

/// <summary>
/// Single source of truth for MCP tool definitions (name/description/schema).
/// Tool execution is handled separately by <see cref="IdeMcpServer"/> and ultimately <see cref="IIdeMcpActions.ExecuteCommandAsync"/>.
/// </summary>
internal static class IdeMcpToolCatalog
{
    private static JsonElement Schema(object schema) => JsonSerializer.SerializeToElement(schema);

    public static List<Tool> BuildTools(bool includeDebugTools)
    {
        // Full list of "rich" MCP tools (typed schemas).
        // Generated proxies are added below for all IdeCommands not covered explicitly.
        var toolsList = IdeMcpToolCatalogFull.BuildRichTools(includeDebugTools);

        if (includeDebugTools)
        {
            // Debug-only tools are already included in BuildRichTools when includeDebugTools = true.
        }

        // Auto-generated proxy tools for all IdeCommands (except those already defined above).
        var existingNames = new HashSet<string>(toolsList.Select(t => t.Name), StringComparer.Ordinal);
        toolsList.AddRange(BuildGeneratedProxyTools(existingNames));

        // Make descriptions self-sufficient (agent-friendly): always include args list from schema.
        foreach (var t in toolsList)
        {
            EnsurePurposePrefix(t);
            EnsureArgsBlockInDescription(t);
            EnsureReturnsAndExampleBlocks(t);
        }

        return toolsList;
    }

    public static IEnumerable<Tool> BuildGeneratedProxyTools(ISet<string> existingToolNames)
    {
        var type = typeof(IdeCommands);
        foreach (var f in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
        {
            if (f.FieldType != typeof(string))
                continue;
            var commandId = (string?)f.GetValue(null);
            if (string.IsNullOrWhiteSpace(commandId))
                continue;

            var toolName = "ide_" + commandId;
            if (existingToolNames.Contains(toolName))
                continue;

            yield return new Tool
            {
                Name = toolName,
                Description = BuildProxyToolDescription(commandId),
                InputSchema = BuildProxyToolInputSchema(commandId)
            };
        }
    }

    private static JsonElement BuildProxyToolInputSchema(string commandId)
    {
        if (!IdeCommandsArgs.TryGetArgs(commandId, out var args) || args.Length == 0)
        {
            // Keep loose schema for unknown/undocumented args.
            return Schema(new
            {
                type = "object",
                properties = new { },
                additionalProperties = true,
                required = Array.Empty<string>()
            });
        }

        // Build JSON schema with known properties but still allow extra keys (future-proof).
        var props = new Dictionary<string, object>(StringComparer.Ordinal);
        var required = new List<string>();

        foreach (var a in args)
        {
            object schema;
            if (a.IsArray)
            {
                schema = new
                {
                    type = "array",
                    items = new { type = a.ItemJsonType ?? "string" }
                };
            }
            else
            {
                schema = new { type = a.JsonType };
            }

            props[a.Name] = schema;
            if (a.Required)
                required.Add(a.Name);
        }

        return Schema(new
        {
            type = "object",
            properties = props,
            additionalProperties = true,
            required = required.ToArray()
        });
    }

    private static string BuildProxyToolDescription(string commandId)
    {
        var summaryLine = IdeCommandsDoc.TryGetSummary(commandId, out var s) && !string.IsNullOrWhiteSpace(s)
            ? $"Назначение: {s}\n\n"
            : "";

        var argsBlock = BuildProxyToolArgsBlock(commandId);

        return
            summaryLine +
            "Прокси-обёртка над ide_execute_command.\n\n" +
            "Эквивалент вызову:\n" +
            $"  ide_execute_command {{ command_id: \"{commandId}\", args: <твои поля> }}\n\n" +
            argsBlock +
            "Если не уверен(а) в полях — см. docs/MCP-PROTOCOL.md или используй ide_execute_command.";
    }

    private static string BuildProxyToolArgsBlock(string commandId)
    {
        if (!IdeCommandsArgs.TryGetArgs(commandId, out var args) || args.Length == 0)
        {
            return
                $"Аргументы: нет (или не задокументированы). Передавай плоский JSON object; поля будут прокинуты в args команды \"{commandId}\".\n\n";
        }

        static string FormatType(IdeCommandsArgs.Arg a)
        {
            if (a.IsArray)
                return $"{a.ItemJsonType ?? "string"}[]";
            return a.JsonType;
        }

        var lines = new List<string>(capacity: 4 + args.Length)
        {
            "Аргументы (плоский JSON object):"
        };

        foreach (var a in args)
        {
            var req = a.Required ? "required" : "optional";
            lines.Add($"  - {a.Name}: {FormatType(a)} ({req})");
        }

        lines.Add("");
        return string.Join("\n", lines) + "\n";
    }

    private static void EnsureArgsBlockInDescription(Tool tool)
    {
        var d = tool.Description ?? "";
        if (d.Contains("\nАргументы", StringComparison.Ordinal) || d.Contains("Аргументы (", StringComparison.Ordinal))
            return;

        if (!TryBuildArgsBlockFromSchema(tool.InputSchema, out var argsBlock))
            return;

        tool.Description = (d.TrimEnd() + "\n\n" + argsBlock).TrimEnd();
    }

    private static void EnsurePurposePrefix(Tool tool)
    {
        var d = (tool.Description ?? "").Trim();
        if (d.Length == 0)
            return;

        // Keep existing structured descriptions intact.
        if (d.StartsWith("Назначение:", StringComparison.Ordinal))
            return;

        tool.Description = $"Назначение: {d}";
    }

    private static bool TryBuildArgsBlockFromSchema(JsonElement inputSchema, out string argsBlock)
    {
        argsBlock = "";

        if (inputSchema.ValueKind != JsonValueKind.Object)
            return false;

        if (!inputSchema.TryGetProperty("properties", out var props) || props.ValueKind != JsonValueKind.Object)
            return false;

        var required = new HashSet<string>(StringComparer.Ordinal);
        if (inputSchema.TryGetProperty("required", out var req) && req.ValueKind == JsonValueKind.Array)
        {
            foreach (var it in req.EnumerateArray())
            {
                if (it.ValueKind == JsonValueKind.String)
                    required.Add(it.GetString()!);
            }
        }

        var propNames = props.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

        if (propNames.Count == 0)
        {
            argsBlock = "Аргументы: нет.\n";
            return true;
        }

        static string GetJsonType(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object)
                return "object";

            if (schema.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String)
            {
                var type = t.GetString()!;
                if (string.Equals(type, "array", StringComparison.Ordinal) &&
                    schema.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object &&
                    items.TryGetProperty("type", out var it) && it.ValueKind == JsonValueKind.String)
                {
                    return it.GetString()! + "[]";
                }

                return type;
            }

            return "object";
        }

        var lines = new List<string>(capacity: 4 + propNames.Count)
        {
            "Аргументы (плоский JSON object):"
        };

        foreach (var name in propNames)
        {
            var schema = props.GetProperty(name);
            var type = GetJsonType(schema);
            var reqLabel = required.Contains(name) ? "required" : "optional";
            lines.Add($"  - {name}: {type} ({reqLabel})");
        }

        argsBlock = string.Join("\n", lines) + "\n";
        return true;
    }

    private static void EnsureReturnsAndExampleBlocks(Tool tool)
    {
        var d = tool.Description ?? "";

        var commandId = tool.Name.StartsWith("ide_", StringComparison.Ordinal) ? tool.Name[4..] : null;
        if (string.IsNullOrWhiteSpace(commandId))
            return;

        if (!d.Contains("\nВозвращает:", StringComparison.Ordinal) && IdeCommandsContract.TryGetReturns(commandId, out var kind))
        {
            var ret = kind switch
            {
                IdeReturnKind.Json => "json",
                IdeReturnKind.Text => "text",
                IdeReturnKind.None => "none",
                _ => "unspecified"
            };
            d = d.TrimEnd() + $"\n\nВозвращает: {ret}.";
        }

        if (!d.Contains("\nExample:", StringComparison.Ordinal) && IdeCommandsContract.TryGetExample(commandId, out var ex) && !string.IsNullOrWhiteSpace(ex))
        {
            d = d.TrimEnd() + "\n\nExample:\n" + ex.Trim();
        }

        tool.Description = d;
    }
}

