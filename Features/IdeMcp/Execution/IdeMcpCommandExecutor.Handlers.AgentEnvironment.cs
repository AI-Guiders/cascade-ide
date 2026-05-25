using System.Text.Json;
using CascadeIDE.Features.Agent.Environment;
using CascadeIDE.Services;

namespace CascadeIDE.Features.IdeMcp.Execution;

internal sealed partial class IdeMcpCommandExecutor
{
    private void RegisterAgentEnvironment(Action<string, Handler> add)
    {
        add(IdeCommands.IdeAgentVerify, async (args, ct) =>
            await _actions.IdeAgentVerifyAsync(
                McpCommandJsonArgs.String(args, "policy"),
                McpCommandJsonArgs.String(args, "sandbox_profile"),
                McpCommandJsonArgs.String(args, "solution_path"),
                ct));

        add(IdeCommands.IdeAgentCancel, async (_, ct) => await _actions.IdeAgentCancelAsync(ct));

        add(IdeCommands.IdeAgentStatus, async (_, ct) => await _actions.IdeAgentStatusAsync(ct));

        add(IdeCommands.IdeAgentLast, async (_, ct) => await _actions.IdeAgentLastAsync(ct));

        add(IdeCommands.IdeAgentSandboxPrepare, async (args, ct) =>
            await _actions.IdeAgentSandboxPrepareAsync(
                McpCommandJsonArgs.String(args, "profile"),
                McpCommandJsonArgs.String(args, "workspace_root"),
                ct));
    }
}
