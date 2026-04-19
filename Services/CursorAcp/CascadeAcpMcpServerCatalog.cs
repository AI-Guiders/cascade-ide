using System.Text.Json;
using AgentClientProtocol;

namespace CascadeIDE.Services.CursorAcp;

/// <summary>
/// Собирает <see cref="McpServer"/> для <see cref="ClientSideConnection.NewSessionAsync"/> из JSON настроек IDE
/// (<see cref="Models.McpSettings.ExternalServersJson"/>): тот же массив, что для автономного режима, плюс опционально элементы в формате ACP (поле <c>type</c>).
/// </summary>
public static class CascadeAcpMcpServerCatalog
{
    /// <summary>Стабильное имя для автоподмешивания IDE MCP (ADR 0048 §7); при совпадении с записью пользователя — приоритет у пользователя.</summary>
    public const string AutoInjectIdeMcpServerName = "cascade-ide";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Пустой или битый JSON → пустой список (как раньше с <c>McpServers = []</c>).
    /// </summary>
    public static McpServer[] FromExternalServersJson(string? externalServersJson)
    {
        if (string.IsNullOrWhiteSpace(externalServersJson))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(externalServersJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];

            var list = new List<McpServer>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object)
                    continue;

                if (el.TryGetProperty("enabled", out var enSkip) && enSkip.ValueKind == JsonValueKind.False)
                    continue;

                if (el.TryGetProperty("type", out var typeEl)
                    && typeEl.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(typeEl.GetString()))
                {
                    var server = JsonSerializer.Deserialize<McpServer>(el.GetRawText(), Json);
                    if (server is not null)
                        list.Add(server);
                    continue;
                }

                var built = TryBuildFromAutonomousSpec(el);
                if (built is not null)
                    list.Add(built);
            }

            return list.ToArray();
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Список для <c>session/new</c>: <paramref name="externalServersJson"/> плюс при необходимости stdio на текущий процесс IDE.
    /// Если сервер с именем <see cref="AutoInjectIdeMcpServerName"/> уже есть у пользователя — автозапись не добавляется.
    /// </summary>
    public static McpServer[] MergeForAcpNewSession(string? externalServersJson, bool acpAutoInjectIdeMcp)
    {
        var user = FromExternalServersJson(externalServersJson);
        if (!acpAutoInjectIdeMcp)
            return user;

        if (HasServerNamed(user, AutoInjectIdeMcpServerName))
            return user;

        var auto = TryCreateAutoIdeMcpStdioServer();
        if (auto is null)
            return user;

        var merged = new McpServer[user.Length + 1];
        merged[0] = auto;
        Array.Copy(user, 0, merged, 1, user.Length);
        return merged;
    }

    /// <summary>Текущий исполняемый файл + <c>--mcp-stdio</c> (как в MCP-PROTOCOL для Cursor).</summary>
    public static StdioMcpServer? TryCreateAutoIdeMcpStdioServer()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
            return null;

        return new StdioMcpServer
        {
            Name = AutoInjectIdeMcpServerName,
            Command = exe,
            Args = ["--mcp-stdio"],
            Env = [],
        };
    }

    private static bool HasServerNamed(McpServer[] servers, string name)
    {
        foreach (var s in servers)
        {
            if (string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static StdioMcpServer? TryBuildFromAutonomousSpec(JsonElement el)
    {
        var name = el.TryGetProperty("name", out var n) ? n.GetString() : null;
        var command = el.TryGetProperty("command", out var c) ? c.GetString() : null;
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
            return null;

        if (!el.TryGetProperty("enabled", out var en) || en.ValueKind != JsonValueKind.True)
            return null;

        var args = new List<string>();
        if (el.TryGetProperty("arguments", out var a) && a.ValueKind == JsonValueKind.Array)
        {
            foreach (var arg in a.EnumerateArray())
            {
                if (arg.ValueKind == JsonValueKind.String)
                {
                    var s = arg.GetString();
                    if (s is not null)
                        args.Add(s);
                }
            }
        }

        var env = new List<EnvVariable>();
        if (el.TryGetProperty("env", out var envEl) && envEl.ValueKind == JsonValueKind.Object)
        {
            foreach (var p in envEl.EnumerateObject())
            {
                if (p.Value.ValueKind != JsonValueKind.String)
                    continue;
                var v = p.Value.GetString() ?? "";
                env.Add(new EnvVariable { Name = p.Name, Value = v });
            }
        }

        return new StdioMcpServer
        {
            Name = name,
            Command = command,
            Args = args.ToArray(),
            Env = env.ToArray(),
        };
    }
}
