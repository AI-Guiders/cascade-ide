using CascadeIDE.Contracts;
using CascadeIDE.Features.Workspace.Application;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Разрешение каталога workspace для палитры и относительных путей.</summary>
[ComputingUnit(note: "command palette goto workspace root")]
public static class CommandPaletteGoToWorkspacePresentation
{
    public static string? TryResolveRoot(string? solutionPath) =>
        WorkspaceBreakpointsRootPresentation.TryResolveExistingDirectory(solutionPath, out var root) ? root : null;

    public static string? TryRelativePath(string workspaceRoot, string fullPath)
    {
        try
        {
            return Path.GetRelativePath(workspaceRoot, fullPath);
        }
        catch
        {
            return null;
        }
    }
}
