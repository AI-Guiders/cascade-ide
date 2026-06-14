#nullable enable

using CascadeIDE.Features.Forge.Lens;

namespace CascadeIDE.Features.Forge.Infrastructure;

public static class ForgeSlashCatalogRefresh
{
    public static async Task RefreshAfterConnectAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var normalized = baseUrl.Trim().TrimEnd('/');
        var token = ForgeLensCredentialResolver.ResolveApiToken(normalized, apiTokenEnv: null);
        await ForgeSlashCatalogOverlay.RefreshAsync(normalized, token, cancellationToken).ConfigureAwait(false);
    }
}
