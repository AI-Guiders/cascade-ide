using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IntercomService.Contracts;

namespace IntercomService.Tests;

[Collection("IntercomApi")]
public sealed class TransportApiTests
{
    private readonly HttpClient _client;

    public TransportApiTests(IntercomWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Dev_token_can_append_and_list_human_event()
    {
        var teamId = "dev-test-team-" + Guid.NewGuid().ToString("N")[..8];
        const string devToken = "dev-intercom-local-change-me";

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", devToken);

        await _client.GetAsync($"/api/v1/teams/{teamId}");
        var topics = await _client.GetFromJsonAsync<List<TopicDto>>($"/api/v1/teams/{teamId}/topics");
        Assert.NotNull(topics);
        Assert.NotEmpty(topics);

        var topicId = topics![0].TopicId;
        var clientEventId = Guid.NewGuid().ToString("N");
        var payload = JsonSerializer.SerializeToElement(new
        {
            message_id = clientEventId,
            role = "user",
            content = "hello from test",
            thread_id = Guid.NewGuid().ToString("N"),
        }, IntercomJson.Web);

        var appendBody = new AppendEventRequest(
            SchemaVersion: 1,
            ClientEventId: clientEventId,
            OccurredAtUtc: DateTimeOffset.UtcNow.ToString("O"),
            EventKind: "message_added",
            Sender: new SenderDto("dev-member", "Dev Operator", "human", "cide"),
            Payload: payload);

        var post = await _client.PostAsJsonAsync($"/api/v1/topics/{topicId}/events", appendBody, IntercomJson.Web);
        post.EnsureSuccessStatusCode();

        var listed = await _client.GetFromJsonAsync<List<TransportEventEnvelopeDto>>(
            $"/api/v1/topics/{topicId}/events");
        Assert.NotNull(listed);
        Assert.Contains(listed!, x => x.ClientEventId == clientEventId);
    }
}

// Fix typo in test - AuthenticationHeaderValue not HeaderValue
