using CascadeIDE.Services;

namespace CascadeIDE.Features.WorkspaceNavigation.Application;

/// <summary>Резолв Bearer для Forge Lens (ADR 0158 §3, образец Intercom secrets + FORGE-ADR-0010).</summary>
public static class ForgeLensCredentialResolver
{
    public static string? ResolveApiToken(string baseUrl, string? apiTokenEnv)
    {
        var fromCide = ForgeLensSecretsStorage.TryGetToken(baseUrl);
        if (!string.IsNullOrEmpty(fromCide))
            return fromCide;

        var fromForgeCli = ForgeSharedCredentialReader.TryGetToken(baseUrl);
        if (!string.IsNullOrEmpty(fromForgeCli))
            return fromForgeCli;

        var envName = apiTokenEnv?.Trim();
        if (string.IsNullOrEmpty(envName))
            return null;

        var fromEnv = Environment.GetEnvironmentVariable(envName)?.Trim();
        return string.IsNullOrEmpty(fromEnv) ? null : fromEnv;
    }
}
