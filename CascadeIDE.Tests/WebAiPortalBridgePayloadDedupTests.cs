using CascadeIDE.Features.WebAiPortal.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WebAiPortalBridgePayloadDedupTests
{
    [Fact]
    public void TryCanonicalKey_WhitespaceInsensitivity_SameKey()
    {
        var loose = "{  \"command_id\"  :  \"ping\" ,\n \"args\": { } }";
        var compact = "{\"command_id\":\"ping\",\"args\":{}}";

        Assert.True(WebAiPortalBridgePayloadDedup.TryCanonicalKey(loose, out var k1));
        Assert.True(WebAiPortalBridgePayloadDedup.TryCanonicalKey(compact, out var k2));
        Assert.Equal(k1, k2);
    }

    [Fact]
    public void TryCanonicalKey_DifferentSemantics_DifferentKey()
    {
        Assert.True(
            WebAiPortalBridgePayloadDedup.TryCanonicalKey("{\"command_id\":\"a\",\"x\":1}", out var k1));
        Assert.True(
            WebAiPortalBridgePayloadDedup.TryCanonicalKey("{\"command_id\":\"a\",\"x\":2}", out var k2));
        Assert.NotEqual(k1, k2);
    }
}
