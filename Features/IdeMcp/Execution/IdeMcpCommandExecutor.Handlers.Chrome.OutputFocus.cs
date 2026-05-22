using System.Text.Json;
using CascadeIDE.Features.IdeMcp.Application;
using CascadeIDE.Models;
using static CascadeIDE.Services.IdeCommands;

namespace CascadeIDE.Features.IdeMcp.Execution;

/// <summary>MCP-хендлеры вывода и фокуса: ping, перезапуск MCP-клиентов, фокус редактора, снимок окна.</summary>
internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterOutputAndFocus(Action<string, Handler> add)
    {
        add(IdePing, async (_, _) => await Task.FromResult(IdeMcpHostOrchestrator.PingJson()));
        add(IdeRestartMcpClients, async (_, ct) => await _vm.RestartMcpClientsForAgentAsync(ct));

        add(FocusEditor, async (_, _) =>
        {
            ((IIdeMcpActions)_vm).FocusEditor();
            return await Task.FromResult("OK");
        });

        Handler captureWindow = async (args, _) =>
        {
            if (_vm.CaptureWindowForMcpAsync is null)
                return "Error: главное окно не привязано к VM (внутренний снимок недоступен).";
            var ws = McpCommandJsonArgs.String(args, "workspace_path");
            var rel = McpCommandJsonArgs.String(args, "output_path");
            var scope = McpCommandJsonArgs.String(args, "scope");
            return await _vm.CaptureWindowForMcpAsync(ws, rel, scope).ConfigureAwait(false);
        };
        add(CaptureWindow, captureWindow);
    }
}
