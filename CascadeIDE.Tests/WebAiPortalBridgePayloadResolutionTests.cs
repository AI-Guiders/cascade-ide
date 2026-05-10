using System.Text.Json;
using CascadeIDE.Features.WebAiPortal.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WebAiPortalBridgePayloadResolutionTests
{
    [Fact]
    public void TryResolve_prefers_fence_over_surrounding_noise()
    {
        const string md = """
            text
            ```json-cascade
            { "command_id": "codebase_index_search", "args": { "query": "x" } }
            ```
            """;
        Assert.True(WebAiPortalBridgePayloadResolution.TryResolvePayload(md, out var json, out var src));
        Assert.Equal(WebAiPortalBridgePayloadResolution.PayloadSourceHint.FencedMarkdown, src);
        Assert.Contains("codebase_index_search", json, StringComparison.Ordinal);
    }

    [Fact]
    public void TryResolve_bare_json_when_no_fence()
    {
        const string s = """{"command_id":"get_editor_state","args":{"max_preview_chars":10000}}""";
        Assert.True(WebAiPortalBridgePayloadResolution.TryResolvePayload(s, out var json, out var src));
        Assert.Equal(WebAiPortalBridgePayloadResolution.PayloadSourceHint.BareJson, src);
        Assert.Equal(s, json);
    }

    [Fact]
    public void TryResolve_rejects_plain_text()
    {
        Assert.False(WebAiPortalBridgePayloadResolution.TryResolvePayload("hello", out _, out _));
        Assert.False(WebAiPortalBridgePayloadResolution.TryParseBareExecuteCommand("{ \"no\": true }", out _));
        Assert.False(WebAiPortalBridgePayloadResolution.TryParseBareExecuteCommand("{\"command_id\":17}", out _));
    }

    [Fact]
    public void Unwrap_invoke_result_strips_json_string_wrapper_like_webview2()
    {
        const string inner = """{"command_id":"get_editor_state","args":{}}""";
        var wrapped = JsonSerializer.Serialize(inner);
        Assert.Equal(inner, WebAiPortalLastCommandDomProbe.UnwrapWrappedJsonString(wrapped));
    }

    [Fact]
    public void Unwrap_passes_through_raw_json_object()
    {
        const string raw = """{"command_id":"x"}""";
        Assert.Equal(raw, WebAiPortalLastCommandDomProbe.UnwrapWrappedJsonString(raw));
    }

    [Fact]
    public void TryGetCommandId_reads_trimmed_string_property()
    {
        const string s = """ {"command_id" : "get_editor_state" } """;
        Assert.True(WebAiPortalBridgePayloadResolution.TryGetCommandId(s, out var id));
        Assert.Equal("get_editor_state", id);

        Assert.False(WebAiPortalBridgePayloadResolution.TryGetCommandId("{} ", out _), "нет command_id");
        Assert.False(WebAiPortalBridgePayloadResolution.TryGetCommandId("", out _), "пустой");
    }
}
