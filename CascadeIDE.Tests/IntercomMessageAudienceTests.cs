#nullable enable

using CascadeIDE.Features.Chat;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.ViewModels;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomMessageAudienceTests
{
    [Fact]
    public void Help_slash_route_is_self_only()
    {
        Assert.True(ChatSlashCommandCatalog.TryResolveInput("/help", out var d, out _));
        Assert.Equal(IntercomMessageAudience.SelfOnly, d.MessageAudience);
    }

    [Fact]
    public void Ide_slash_routes_default_to_self_only()
    {
        Assert.True(ChatSlashCommandCatalog.TryResolveInput("/solution open", out var d, out _));
        Assert.Equal(IntercomMessageAudience.SelfOnly, d.MessageAudience);
        Assert.Equal("open_solution_dialog", d.CommandId);
    }

    [Fact]
    public void Payload_round_trips_audience_and_slash_fields()
    {
        var vm = ChatMessageViewModel.CreateSlashCommand(
            "/help",
            null,
            audience: IntercomMessageAudience.SelfOnly);
        vm.ApplySlashCommandResult(new ChatSlashCommandRunResult(true, true, "/help", null, "ok"));

        var payload = ChatHistoryPayloadMapping.ToMessagePayload(vm);
        Assert.Equal(IntercomMessageAudience.SelfOnly, payload.Audience);

        var rows = ChatHistoryMessageProjector.Project(
            [
                new ChatHistoryEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    ChatHistoryEventKind.MessageCompleted,
                    ChatHistoryJson.Serialize(payload)),
            ],
            Guid.NewGuid());

        Assert.Single(rows);
        Assert.Equal(IntercomMessageAudience.SelfOnly, rows[0].Audience);
        Assert.Equal("/help", rows[0].SlashCommandPath);
        Assert.Equal(ChatSlashCommandStatus.Succeeded, rows[0].SlashCommandStatus);
    }
}
