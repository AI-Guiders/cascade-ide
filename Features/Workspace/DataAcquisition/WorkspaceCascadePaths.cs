#nullable enable
using CascadeIDE.Contracts;

namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>DAL: пути и discovery для <c>.cascade/workspace.toml</c> (ADR 0102) — без typed overlay.</summary>
[IoBoundary]
public static class WorkspaceCascadePaths
{
    public static string GetWorkspaceTomlPath(string workspaceRoot) =>
        Path.Combine(workspaceRoot.Trim(), ".cascade", "workspace.toml");

    /// <summary>Walk up from cwd: first dir with <c>.cascade/workspace.toml</c>, else first with <c>.git</c>.</summary>
    public static string? TryDiscoverWorkspaceRoot(string? startDirectory = null, int maxLevels = 10)
    {
        try
        {
            var dir = new DirectoryInfo(
                string.IsNullOrWhiteSpace(startDirectory)
                    ? Environment.CurrentDirectory
                    : startDirectory);
            for (var i = 0; i < maxLevels && dir is not null; i++, dir = dir.Parent)
            {
                if (File.Exists(GetWorkspaceTomlPath(dir.FullName)))
                    return dir.FullName;
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
                    return dir.FullName;
            }
        }
        catch
        {
            // ignore discovery failures
        }

        return null;
    }
}
