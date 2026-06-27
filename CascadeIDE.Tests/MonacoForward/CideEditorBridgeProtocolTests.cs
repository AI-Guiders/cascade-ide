using System.Text.Json;
using CascadeIDE.Features.Editor.Application.Monaco;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CideEditorBridgeProtocolTests
{
    [Fact]
    public void TryParseInbound_capability_request_round_trips_fields()
    {
        const string json = """
            {"type":"capability/completion","requestId":42,"line":3,"column":5}
            """;
        var msg = CideEditorBridgeJson.TryParseInbound(json);
        Assert.NotNull(msg);
        Assert.Equal(CideEditorBusManifest.Capabilities.Completion, msg!.Type);
        Assert.Equal(42, msg.RequestId);
        Assert.Equal(3, msg.Line);
        Assert.Equal(5, msg.Column);
    }

    [Fact]
    public void TryParseInbound_didChange_carries_version_and_text()
    {
        const string json = """
            {"type":"editor/didChange","version":7,"text":"abc","caretOffset":2}
            """;
        var msg = CideEditorBridgeJson.TryParseInbound(json);
        Assert.NotNull(msg);
        Assert.Equal(7, msg!.Version);
        Assert.Equal("abc", msg.Text);
        Assert.Equal(2, msg.CaretOffset);
    }

    [Fact]
    public void WrapOutbound_applyEdits_includes_expectedVersion()
    {
        var payload = new CideEditorApplyEditsMessage(
            [new CideEditorApplyEdit(1, 2, "x")],
            ExpectedVersion: 9);
        var wrapped = CideEditorBridgeJson.WrapOutbound(CideEditorBridgeTypes.ApplyEdits, payload);
        using var doc = JsonDocument.Parse(wrapped);
        Assert.Equal(CideEditorBridgeTypes.ApplyEdits, doc.RootElement.GetProperty("type").GetString());
        var inner = doc.RootElement.GetProperty("payload");
        Assert.Equal(9, inner.GetProperty("expectedVersion").GetInt32());
        Assert.Equal(1, inner.GetProperty("edits")[0].GetProperty("startOffset").GetInt32());
    }

    [Fact]
    public void FromFilePath_maps_common_extensions()
    {
        Assert.Equal("csharp", CideEditorLanguageIds.FromFilePath(@"D:\a\b.cs"));
        Assert.Equal("toml", CideEditorLanguageIds.FromFilePath(@"settings.toml"));
        Assert.Equal("plaintext", CideEditorLanguageIds.FromFilePath(@"readme.xyz"));
    }
}
