#nullable enable

using CascadeIDE.Contracts.Experimental;

namespace CascadeIDE.Features.Forge.Mcp;

/// <summary>Adapts forge MCP registration to IdeMcp executor handler map.</summary>
internal sealed class McpHandlerRegistrarAdapter(Action<string, McpCommandHandler> add) : IMcpHandlerRegistrar
{
    public void Add(string commandId, McpCommandHandler handler) => add(commandId, handler);
}
