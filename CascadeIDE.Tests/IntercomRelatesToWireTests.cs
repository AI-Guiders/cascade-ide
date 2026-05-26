using System.Text.Json;
using CascadeIDE.Features.Intercom.Transport;
using CascadeIDE.Models.AgentChat;
using CascadeIDE.Models.Intercom;
using CascadeIDE.Services.Intercom;
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
    public void EnrichForWire_Disjoint_AddsMultipleRelatesToAndOrdinalSegments()
    {
        var anchor = new AttachmentAnchor { File = "src/Foo.cs", LineStart = 1, LineEnd = 2 };
        var range = IntercomMessageRangeRelatedSupport.CreatePayload(
            Guid.NewGuid().ToString("N"),
            [
                new ChatHistoryMessageOrdinalSegment(3, 5),
                new ChatHistoryMessageOrdinalSegment(8, 15),
            ],
            anchor,
            "slash");
        var json = JsonSerializer.Serialize(range, IntercomTransportJson.Web);
        var element = JsonSerializer.Deserialize<JsonElement>(json, IntercomTransportJson.Web);

        var wire = IntercomTransportPayloadEnricher.EnrichForWire(ChatHistoryEventKind.MessageRangeRelated, element);

        Assert.True(wire.TryGetProperty("relates_to", out var relates));
        Assert.Equal(2, relates.GetArrayLength());
        Assert.Equal(3, relates[0].GetProperty("ordinal_range").GetProperty("start_ordinal").GetInt32());
        Assert.Equal(5, relates[0].GetProperty("ordinal_range").GetProperty("end_ordinal").GetInt32());
        Assert.Equal(8, relates[1].GetProperty("ordinal_range").GetProperty("start_ordinal").GetInt32());
        Assert.Equal(15, relates[1].GetProperty("ordinal_range").GetProperty("end_ordinal").GetInt32());

        Assert.True(wire.TryGetProperty("ordinal_segments", out var segments));
        Assert.Equal(2, segments.GetArrayLength());
    }

    [Fact]
    public void NormalizeInbound_DisjointFromRelatesTo_FillsOrdinalSegments()
    {
        var anchor = new AttachmentAnchor { File = "src/Foo.cs" };
        var wireJson = JsonSerializer.Serialize(new
        {
            thread_id = Guid.NewGuid().ToString("N"),
            start_ordinal = 3,
            end_ordinal = 15,
            code_ref = anchor,
            source = "slash",
            relates_to = new object[]
            {
                new
                {
                    target_kind = "code",
                    relation = "documents",
                    code_ref = anchor,
                    ordinal_range = new { start_ordinal = 3, end_ordinal = 5 },
                    confidence = "explicit",
                },
                new
                {
                    target_kind = "code",
                    relation = "documents",
                    code_ref = anchor,
                    ordinal_range = new { start_ordinal = 8, end_ordinal = 15 },
                    confidence = "explicit",
                },
            },
        }, IntercomTransportJson.Web);

        var normalized = IntercomTransportPayloadNormalizer.NormalizeInbound(
            "message_range_related",
            wireJson);

        using var doc = JsonDocument.Parse(normalized);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("ordinal_segments", out var segments));
        Assert.Equal(2, segments.GetArrayLength());
        Assert.Equal(3, segments[0].GetProperty("start_ordinal").GetInt32());
        Assert.Equal(5, segments[0].GetProperty("end_ordinal").GetInt32());
        Assert.Equal(8, segments[1].GetProperty("start_ordinal").GetInt32());
        Assert.Equal(15, segments[1].GetProperty("end_ordinal").GetInt32());
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
