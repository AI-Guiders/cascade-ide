#nullable enable
using System.Diagnostics.CodeAnalysis;
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Workspace.Application;

/// <summary>
/// Корень каталога workspace через <see cref="BreakpointsFileService.GetWorkspaceRoot"/> (палитра GoTo, MCP search_workspace_text).
/// </summary>
[ComputingUnit("workspace-root-breakpoints")]
public static class WorkspaceBreakpointsRootPresentation
{
    public static bool TryResolveExistingDirectory(
        string? solutionPath,
        [NotNullWhen(true)] out string? root)
    {
        root = null;
        if (string.IsNullOrWhiteSpace(solutionPath))
            return false;

        var candidate = BreakpointsFileService.GetWorkspaceRoot(solutionPath);
        if (string.IsNullOrEmpty(candidate) || !Directory.Exists(candidate))
            return false;

        root = candidate;
        return true;
    }
}
