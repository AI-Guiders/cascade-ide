using CascadeIDE.Features.IdeMcp.Application;

namespace CascadeIDE.ViewModels;

/// <summary>Жизненный цикл IDE MCP-хоста: <c>ide_ping</c>, перезапуск внешних MCP и stdio-сессии Cursor ACP.</summary>
public partial class MainWindowViewModel
{
    /// <inheritdoc cref="IdeMcpHostOrchestrator.PingJson"/>
    public static string PingIdeMcpHostJson() => IdeMcpHostOrchestrator.PingJson();

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
        return IdeMcpHostOrchestrator.RestartClientsOkJson();
    }
}
