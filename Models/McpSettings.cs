namespace CascadeIDE.Models;

/// <summary>MCP-настройки (<c>[mcp]</c> в <c>settings.toml</c>).</summary>
public sealed class McpSettings
{
    /// <summary>JSON-массив внешних MCP-серверов для автономного режима и для Cursor ACP (<c>session/new</c>, поле <c>mcpServers</c>) (TOML: <c>external_servers_json</c>).</summary>
    public string ExternalServersJson { get; set; } = "[]";

    /// <summary>
    /// Подмешивать в <c>session/new</c> для Cursor ACP запись stdio на текущий процесс IDE с <c>--mcp-stdio</c> (имя <c>cascade-ide</c>),
    /// если пользователь не задал сервер с тем же именем в <see cref="ExternalServersJson"/> (TOML: <c>acp_auto_inject_ide_mcp</c>).
    /// </summary>
    public bool AcpAutoInjectIdeMcp { get; set; } = true;
}
