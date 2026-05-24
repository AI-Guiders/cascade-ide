using CascadeIDE.Features.Intercom.Transport;
using Xunit;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;

namespace CascadeIDE.Tests;

public sealed class IntercomTransportPublishRulesTests
{
    [Fact]
    public void ShouldPublish_HumanChannelUserMessage()
    {
        var payload = new ChatHistoryMessagePayload(
            Guid.NewGuid().ToString("N"),
            "user",
            "hello",
            Guid.NewGuid().ToString("N"));
        var json = System.Text.Json.JsonSerializer.Serialize(payload, IntercomTransportJson.Web);
        Assert.True(IntercomTransportPublishRules.ShouldPublish(ChatHistoryEventKind.MessageAdded, json));
    }

    [Fact]
    public void ShouldPublish_SkipsSelfOnlyAndSlash()
    {
        var self = new ChatHistoryMessagePayload(
            Guid.NewGuid().ToString("N"),
            "user",
            "x",
            Guid.NewGuid().ToString("N"),
            Audience: IntercomMessageAudience.SelfOnly);
        var slash = new ChatHistoryMessagePayload(
            Guid.NewGuid().ToString("N"),
            "user",
            "x",
            Guid.NewGuid().ToString("N"),
            SlashCommandPath: "/help");
        var selfJson = System.Text.Json.JsonSerializer.Serialize(self, IntercomTransportJson.Web);
        var slashJson = System.Text.Json.JsonSerializer.Serialize(slash, IntercomTransportJson.Web);
        Assert.False(IntercomTransportPublishRules.ShouldPublish(ChatHistoryEventKind.MessageAdded, selfJson));
        Assert.False(IntercomTransportPublishRules.ShouldPublish(ChatHistoryEventKind.MessageAdded, slashJson));

        var fork = new ChatHistoryThreadForkedPayload(
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N"));
        var forkJson = System.Text.Json.JsonSerializer.Serialize(fork, IntercomTransportJson.Web);
        Assert.True(IntercomTransportPublishRules.ShouldPublish(ChatHistoryEventKind.ThreadForked, forkJson));
    }
}
