#nullable enable

using CascadeIDE.Features.WorkspaceNavigation.Application;

namespace CascadeIDE.Services.Forge;

public static class ForgeSlashCatalogRefresh
{
    public static async Task RefreshAfterConnectAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var normalized = baseUrl.Trim().TrimEnd('/');
        var token = ForgeLensCredentialResolver.ResolveApiToken(normalized, apiTokenEnv: null);
        await ForgeSlashCatalogOverlay.RefreshAsync(normalized, token, cancellationToken).ConfigureAwait(false);
    }
}
