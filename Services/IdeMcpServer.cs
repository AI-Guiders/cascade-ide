using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace CascadeIDE.Services;

public static class IdeMcpServer
{
    public static McpServerOptions BuildOptions(IIdeMcpActions actions)
    {
        static JsonElement Schema(object schema) => JsonSerializer.SerializeToElement(schema);

        var toolsList = new List<Tool>
        {
            new()
            {
                Name = "ide_open_file",
                Description = "Открыть файл в редакторе IDE по пути.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { path = new { type = "string", description = "Полный путь к файлу." } },
                    required = new[] { "path" }
                })
            },
            new()
            {
                Name = "ide_set_breakpoint",
                Description = "Поставить брейкпоинт в файле на указанной строке.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        file_path = new { type = "string" },
                        line = new { type = "integer", description = "Номер строки (1-based)." },
                        condition = new { type = "string", description = "Опциональное условие." }
                    },
                    required = new[] { "file_path", "line" }
                })
            },
            new()
            {
                Name = "ide_show_preview",
                Description = "Показать превью (дифф/изменения) в IDE.",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new
                    {
                        title = new { type = "string" },
                        content = new { type = "string" }
                    },
                    required = new[] { "title", "content" }
                })
            },
            new()
            {
                Name = "ide_request_confirmation",
                Description = "Запросить подтверждение у пользователя. Возвращает ответ пользователя (ok/cancel или текст).",
                InputSchema = Schema(new
                {
                    type = "object",
                    properties = new { message = new { type = "string" } },
                    required = new[] { "message" }
                })
            }
        };

        return new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "CascadeIDE", Version = "0.1.0" },
            ProtocolVersion = "2024-11-05",
            Capabilities = new ServerCapabilities { Tools = new ToolsCapability { ListChanged = false } },
            Handlers = new McpServerHandlers
            {
                ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = toolsList }),
                CallToolHandler = async (request, cancellationToken) =>
                {
                    var name = request.Params?.Name ?? "";
                    var args = request.Params?.Arguments is IReadOnlyDictionary<string, JsonElement> a ? a : null;
                    try
                    {
                        var text = name switch
                        {
                            "ide_open_file" => CallOpenFile(actions, args),
                            "ide_set_breakpoint" => CallSetBreakpoint(actions, args),
                            "ide_show_preview" => CallShowPreview(actions, args),
                            "ide_request_confirmation" => await CallRequestConfirmation(actions, args, cancellationToken),
                            _ => $"Unknown tool: {name}"
                        };
                        return new CallToolResult { Content = [new TextContentBlock { Text = text }] };
                    }
                    catch (Exception ex)
                    {
                        return new CallToolResult { Content = [new TextContentBlock { Text = "Error: " + ex.Message }], IsError = true };
                    }
                }
            }
        };
    }

    private static string CallOpenFile(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        var path = args is not null && args.TryGetValue("path", out var p) ? p.GetString() : null;
        if (string.IsNullOrEmpty(path))
            return "Missing path";
        actions.OpenFile(path);
        return "OK";
    }

    private static string CallSetBreakpoint(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null || !args.TryGetValue("file_path", out var fp) || !args.TryGetValue("line", out var ln))
            return "Missing arguments";
        var filePath = fp.GetString();
        var line = ln.GetInt32();
        var condition = args.TryGetValue("condition", out var c) ? c.GetString() : null;
        if (string.IsNullOrEmpty(filePath))
            return "Missing file_path";
        actions.SetBreakpoint(filePath, line, condition);
        return "OK";
    }

    private static string CallShowPreview(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args)
    {
        if (args is null)
            return "Missing arguments";
        var title = args.TryGetValue("title", out var t) ? t.GetString() ?? "" : "";
        var content = args.TryGetValue("content", out var c) ? c.GetString() ?? "" : "";
        actions.ShowPreview(title, content);
        return "OK";
    }

    private static async Task<string> CallRequestConfirmation(IIdeMcpActions actions, IReadOnlyDictionary<string, JsonElement>? args, CancellationToken ct)
    {
        var message = args is not null && args.TryGetValue("message", out var m) ? m.GetString() ?? "" : "";
        return await actions.RequestConfirmationAsync(message, ct);
    }
}
