using CascadeIDE.Features.Editor.Application.Monaco;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class CideEditorBusManifestTests
{
    [Theory]
    [InlineData("debug-line", CideEditorBusManifest.SetIds.DebugLine)]
    [InlineData("agent-reveal", CideEditorBusManifest.SetIds.AgentReveal)]
    [InlineData("cf-gutter", CideEditorBusManifest.SetIds.CfGutter)]
    [InlineData("diagnostics", CideEditorBusManifest.SetIds.Diagnostics)]
    public void Normalize_setId_maps_legacy(string raw, string expected) =>
        Assert.Equal(expected, CideEditorBusManifest.SetIds.Normalize(raw));

    [Theory]
    [InlineData(CideEditorBusManifest.Legacy.RequestCompletion, CideEditorBusManifest.Capabilities.Completion)]
    [InlineData(CideEditorBusManifest.Capabilities.Hover, CideEditorBusManifest.Capabilities.Hover)]
    public void NormalizeInboundType_maps_legacy_capabilities(string raw, string expected) =>
        Assert.Equal(expected, CideEditorBusManifest.NormalizeInboundType(raw));

    [Theory]
    [InlineData(CideEditorBusManifest.Capabilities.SemanticTokens, true)]
    [InlineData(CideEditorBusManifest.Capabilities.SemanticTokensResult, false)]
    public void IsCapabilityRequest_and_result(string type, bool isRequest)
    {
        if (isRequest)
            Assert.True(CideEditorBusManifest.IsCapabilityRequest(type));
        else
            Assert.True(CideEditorBusManifest.IsCapabilityResult(type));
    }
}
