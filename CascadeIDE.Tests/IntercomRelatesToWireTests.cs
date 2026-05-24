using System.Text.Json;
using CascadeIDE.Features.Intercom.Transport;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class IntercomRelatesToWireTests
{
    [Fact]
    public void EnrichForWire_MessageRangeRelated_AddsRelatesTo()
    {
        var anchor = new AttachmentAnchor { File = "src/Foo.cs", LineStart = 10, LineEnd = 12 };
        var range = new ChatHistoryMessageRangeRelatedPayload(
            Guid.NewGuid().ToString("N"),
            3,
            5,
            anchor,
            "slash");
        var json = JsonSerializer.Serialize(range, IntercomTransportJson.Web);
        var element = JsonSerializer.Deserialize<JsonElement>(json, IntercomTransportJson.Web);

        var wire = IntercomTransportPayloadEnricher.EnrichForWire(ChatHistoryEventKind.MessageRangeRelated, element);

        Assert.True(wire.TryGetProperty("relates_to", out var relates));
        Assert.Equal(JsonValueKind.Array, relates.ValueKind);
        Assert.Equal(1, relates.GetArrayLength());
        var link = relates[0];
        Assert.Equal("code", link.GetProperty("target_kind").GetString());
        Assert.Equal("documents", link.GetProperty("relation").GetString());
        Assert.Equal("explicit", link.GetProperty("confidence").GetString());
        Assert.Equal(3, link.GetProperty("ordinal_range").GetProperty("start_ordinal").GetInt32());
        Assert.Equal(5, link.GetProperty("ordinal_range").GetProperty("end_ordinal").GetInt32());
        Assert.Equal("src/Foo.cs", link.GetProperty("code_ref").GetProperty("file").GetString());
    }

    [Fact]
    public void EnrichForWire_MessageAdded_Unchanged()
    {
        var msg = new ChatHistoryMessagePayload(
            Guid.NewGuid().ToString("N"),
            "user",
            "hi",
            Guid.NewGuid().ToString("N"));
        var json = JsonSerializer.Serialize(msg, IntercomTransportJson.Web);
        var element = JsonSerializer.Deserialize<JsonElement>(json, IntercomTransportJson.Web);

        var wire = IntercomTransportPayloadEnricher.EnrichForWire(ChatHistoryEventKind.MessageAdded, element);

        Assert.False(wire.TryGetProperty("relates_to", out _));
    }
}
