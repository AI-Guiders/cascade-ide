using System.Text.Json;
using CascadeIDE.Features.Intercom.Transport;
using CascadeIDE.Models.AgentChat;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomTransportPublishRulesTests
{
    [Fact]
    public void Assistant_message_resolves_to_agent_sender_role()
    {
        var payload = JsonSerializer.Serialize(new ChatHistoryMessagePayload(
            MessageId: Guid.NewGuid().ToString("N"),
            Role: "assistant",
            Content: "hi",
            ThreadId: Guid.NewGuid().ToString("N")));

        var role = IntercomTransportPublishRules.ResolveWireSenderRole(
            payload,
            ChatHistoryEventKind.MessageCompleted);

        Assert.Equal("agent", role);
    }
}
