using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntercomService.Contracts;

namespace IntercomService.Tests;

[Collection("IntercomApi")]
public sealed class AgentTransportAppendTests
{
    private readonly HttpClient _client;

    public AgentTransportAppendTests(IntercomWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Admin_can_provision_agent_account()
    {
        var teamId = "dev-agent-team-" + Guid.NewGuid().ToString("N")[..8];
        const string devToken = "dev-intercom-local-change-me";
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", devToken);

        (await _client.GetAsync($"/api/v1/teams/{teamId}")).EnsureSuccessStatusCode();

        var agent = await _client.PostAsJsonAsync(
            $"/api/v1/teams/{teamId}/agents",
            new { display_name = "Nova" },
            IntercomJson.Web);
        agent.EnsureSuccessStatusCode();
        var agentDto = await agent.Content.ReadFromJsonAsync<AgentDto>(IntercomJson.Web);
        Assert.NotNull(agentDto);
        Assert.StartsWith("agent-", agentDto!.MemberId, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(agentDto.CredentialToken));
    }
}
