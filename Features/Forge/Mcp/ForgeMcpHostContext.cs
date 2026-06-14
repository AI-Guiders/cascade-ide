#nullable enable

using CascadeIDE.Services;
using CascadeIDE.ViewModels;

namespace CascadeIDE.Features.Forge.Mcp;

/// <summary>Host dependencies for forge MCP handlers (spine passes executor context).</summary>
internal sealed class ForgeMcpHostContext
{
    public required MainWindowViewModel Vm { get; init; }

    public required IIdeMcpActions Actions { get; init; }

    public required Func<string?> TryGetWorkspaceRoot { get; init; }

    public required Func<string?> TryGetAttachSolutionPath { get; init; }
}
