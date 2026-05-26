using IntercomService.Contracts;
using IntercomService.Options;
using Microsoft.Extensions.Options;

namespace IntercomService.Services;

public sealed class AuthProviderRegistry(
    IOptions<GitHubAuthOptions> githubOptions,
    IOptions<OidcAuthOptions> oidcOptions)
{
    public IReadOnlyList<AuthProviderDto> ListProviders()
    {
        var list = new List<AuthProviderDto>();
        var github = githubOptions.Value;
        if (!string.IsNullOrWhiteSpace(github.ClientId) && !string.IsNullOrWhiteSpace(github.ClientSecret))
            list.Add(new AuthProviderDto("github", "GitHub", true));

        var oidc = oidcOptions.Value;
        if (!string.IsNullOrWhiteSpace(oidc.Authority)
            && !string.IsNullOrWhiteSpace(oidc.ClientId)
            && !string.IsNullOrWhiteSpace(oidc.ClientSecret))
        {
            var id = string.IsNullOrWhiteSpace(oidc.ProviderId) ? "oidc" : oidc.ProviderId.Trim();
            var name = string.IsNullOrWhiteSpace(oidc.DisplayName) ? id : oidc.DisplayName.Trim();
            list.Add(new AuthProviderDto(id, name, true));
        }

        return list;
    }
}
