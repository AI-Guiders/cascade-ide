namespace CascadeIDE.Services;

/// <summary>
/// Клиент к внешним MCP (debug-mcp, RoslynMCP). Модель может вызывать их через тулы — этот сервис выполняет вызовы.
/// Пока заглушка: реальное подключение (stdio/порт) и вызов тулов — в следующих итерациях.
/// </summary>
public class McpClientService
{
    /// <summary>Вызвать тул debug-mcp (debug_set_breakpoints, debug_launch, debug_continue и т.д.).</summary>
    public Task<string> CallDebugMcpAsync(string toolName, IReadOnlyDictionary<string, object>? arguments, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"[debug-mcp] {toolName}: not connected (stub). Подключите dotnet-debug-mcp как MCP-сервер и настройте вызовы.");
    }

    /// <summary>Вызвать тул RoslynMCP (roslyn_find_usages, roslyn_rename, roslyn_get_document_symbols и т.д.).</summary>
    public Task<string> CallRoslynMcpAsync(string toolName, IReadOnlyDictionary<string, object>? arguments, CancellationToken cancellationToken = default)
    {
        return Task.FromResult($"[roslyn-mcp] {toolName}: not connected (stub). Подключите roslyn-mcp и настройте вызовы.");
    }
}
