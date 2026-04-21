using System.Text.Json;

namespace CascadeIDE.ViewModels;

/// <summary>Жизненный цикл IDE MCP-хоста: <c>ide_ping</c>, перезапуск внешних MCP и stdio-сессии Cursor ACP.</summary>
public partial class MainWindowViewModel
{
    /// <summary>JSON для MCP <c>ide_ping</c>: живость хоста и PID процесса IDE.</summary>
    public static string PingIdeMcpHostJson() =>
        JsonSerializer.Serialize(new
        {
            ok = true,
            kind = "cascade_ide_mcp_host",
            utc = DateTimeOffset.UtcNow,
            pid = Environment.ProcessId,
        });

    /// <summary>Пересоздать клиентов внешних MCP и сбросить stdio-сессию Cursor ACP.</summary>
    public async Task<string> RestartMcpClientsForAgentAsync(CancellationToken cancellationToken = default)
    {
        Autonomous.CancelForHostReconfiguration();
        await _mcpClientService.DisposeAsync().ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        _mcpClientService = new Services.McpClientService(Services.McpExternalServersJsonResolver.ResolveEffectiveJson(_settings));
        _autonomousAgentService = CreateAutonomousAgentService(_mcpClientService);
        Autonomous.ReplaceAgentService(_autonomousAgentService);
        ChatPanel.DisposeCursorAcpSession();
        return JsonSerializer.Serialize(new { ok = true, restarted = true });
    }
}
