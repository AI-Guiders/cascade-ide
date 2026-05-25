using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntercomService.Contracts;

namespace IntercomService.Tests;

[Collection("IntercomApi")]
public sealed class IdentityApiTests
{
    private readonly HttpClient _client;

    public IdentityApiTests(IntercomWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Auth_providers_returns_list()
    {
        var providers = await _client.GetFromJsonAsync<List<AuthProviderDto>>("/api/v1/auth/providers");
        Assert.NotNull(providers);
    }

    [Fact]
    public async Task Dev_token_me_lists_teams_with_roles()
    {
        const string devToken = "dev-intercom-local-change-me";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", devToken);

        var teamId = "identity-me-" + Guid.NewGuid().ToString("N")[..8];
        var teamResponse = await _client.GetAsync($"/api/v1/teams/{teamId}");
        teamResponse.EnsureSuccessStatusCode();

        var me = await _client.GetFromJsonAsync<MeResponse>("/api/v1/auth/me");
        Assert.NotNull(me);
        Assert.Contains(me!.Teams, x => x.TeamId == teamId && x.TeamRole == "admin");
    }

    [Fact]
    public async Task Workspace_resolve_empty_without_projects()
    {
        const string devToken = "dev-intercom-local-change-me";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", devToken);

        await _client.GetAsync("/api/v1/teams/workspace-test-team");

        var response = await _client.GetFromJsonAsync<WorkspaceContextResponse>(
            "/api/v1/resolve/workspace-context?repo_url=" + Uri.EscapeDataString("github.com/example/repo"));
        Assert.NotNull(response);
        Assert.Equal(["github.com/example/repo"], response!.NormalizedRepoUrls);
        Assert.Empty(response.Teams);
    }
}
