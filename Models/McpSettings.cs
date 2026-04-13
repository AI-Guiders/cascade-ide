namespace CascadeIDE.Models;

/// <summary>MCP-настройки (<c>[mcp]</c> в <c>settings.toml</c>).</summary>
public sealed class McpSettings
{
    /// <summary>Включить встроенный MCP-сервер IDE при запуске с <c>--mcp-stdio</c> (TOML: <c>stdio_server_enabled</c>).</summary>
    public bool StdioServerEnabled { get; set; } = true;

    /// <summary>JSON-массив внешних MCP-серверов для автономного режима (TOML: <c>external_servers_json</c>).</summary>
    public string ExternalServersJson { get; set; } = "[]";
}
