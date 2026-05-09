using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Search.Application;

/// <summary>Разрешение каталога workspace для палитры и относительных путей.</summary>
[ComputingUnit(note: "command palette goto workspace root")]
public static class CommandPaletteGoToWorkspacePresentation
{
    public static string? TryResolveRoot(string? solutionPath)
    {
        if (string.IsNullOrWhiteSpace(solutionPath))
            return null;
        var root = BreakpointsFileService.GetWorkspaceRoot(solutionPath);
        return string.IsNullOrEmpty(root) || !Directory.Exists(root) ? null : root;
    }

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
