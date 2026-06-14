using CascadeIDE.Contracts.Experimental;
using CascadeIDE.Features.Forge;
using CascadeIDE.Features.Forge.Mcp;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.IdeMcp.Execution;

internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterForge(Action<string, Handler> add)
    {
        var ctx = new ForgeMcpHostContext
        {
            Vm = _vm,
            Actions = _actions,
            TryGetWorkspaceRoot = () => TryGetWorkspaceRoot(_actions),
            TryGetAttachSolutionPath = TryGetAttachSolutionPath,
        };

        ForgeFeatureModule.Instance.BindMcpHost(ctx);
        ForgeFeatureModule.Instance.RegisterMcpHandlers(
            ctx,
            new McpHandlerRegistrarAdapter((id, handler) => add(id, (args, ct) => handler(args, ct))));
    }
}
