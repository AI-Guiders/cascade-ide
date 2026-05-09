using System.Text.Json;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.IdeMcp.Application;

/// <summary>JSON и полезная нагрузка для MCP-жизненного цикла IDE-хоста (ping, перезапуск клиентов).</summary>
[ApplicationOrchestrator]
public static class IdeMcpHostOrchestrator
{
    public static string PingJson() =>
        JsonSerializer.Serialize(new
        {
            ok = true,
            kind = "cascade_ide_mcp_host",
            utc = DateTimeOffset.UtcNow,
            pid = Environment.ProcessId,
        });

    public static string RestartClientsOkJson() =>
        JsonSerializer.Serialize(new { ok = true, restarted = true });
}
