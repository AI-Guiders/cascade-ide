using CascadeIDE.Features.Workspace;
using CascadeIDE.Features.Workspace.DataAcquisition;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

internal static class ForgeLensWorkspaceConfig
{
    internal static (string? BaseUrl, string? Repo) TryResolve(string? workspaceRoot)
    {
        var toml = RepositoryWorkspaceTomlLoader.TryLoad(workspaceRoot);
        var forge = toml?.Workspace?.Forge;
        var baseUrl = (forge?.BaseUrl ?? "").Trim().TrimEnd('/');
        var repo = (forge?.Repo ?? "").Trim();
        if (baseUrl.Length == 0 || repo.Length == 0)
            return (null, null);
        return (baseUrl, repo);
    }

    internal static string? TryResolveBaseUrl(string? workspaceRoot) =>
        TryResolve(workspaceRoot).BaseUrl;
}
