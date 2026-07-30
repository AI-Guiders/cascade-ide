#nullable enable

using CascadeIDE.Services;

namespace CascadeIDE.Features.Workspace.DataAcquisition;

/// <summary>Чтение <c>.cascade/workspace.toml</c> из корня workspace (ADR 0102).</summary>
public static class RepositoryWorkspaceTomlLoader
{
    public static string GetWorkspaceTomlPath(string workspaceRoot) =>
        WorkspaceCascadePaths.GetWorkspaceTomlPath(workspaceRoot);

    public static RepositoryWorkspaceToml? TryLoad(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        var path = GetWorkspaceTomlPath(workspaceRoot);
        if (!WorkspaceTextFileReader.TryReadAllText(path, out var text))
            return null;

        try
        {
            return CascadeTomlSerializer.Deserialize<RepositoryWorkspaceToml>(text);
        }
        catch
        {
            return null;
        }
    }
}
