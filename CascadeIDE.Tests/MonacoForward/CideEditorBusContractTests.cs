using System.Text.Json;
using CascadeIDE.Features.Editor.Application.Monaco;
using Xunit;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CideEditorBusContractTests
{
    [Fact]
    public void BusManifestJson_matches_CSharp_setIds()
    {
        var root = LoadManifestRoot();
        var setIds = root.GetProperty("setIds");

        Assert.Equal(CideEditorBusManifest.SetIds.Diagnostics, setIds.GetProperty("diagnostics").GetString());
        Assert.Equal(CideEditorBusManifest.SetIds.Highlights, setIds.GetProperty("highlights").GetString());
        Assert.Equal(CideEditorBusManifest.SetIds.Breakpoints, setIds.GetProperty("breakpoints").GetString());
        Assert.Equal(CideEditorBusManifest.SetIds.DebugLine, setIds.GetProperty("debugLine").GetString());
        Assert.Equal(CideEditorBusManifest.SetIds.AgentReveal, setIds.GetProperty("agentReveal").GetString());
        Assert.Equal(CideEditorBusManifest.SetIds.CfGutter, setIds.GetProperty("cfGutter").GetString());
    }

    [Fact]
    public void BusManifestJson_matches_CSharp_capabilities()
    {
        var caps = LoadManifestRoot().GetProperty("capabilities");

        Assert.Equal(CideEditorBusManifest.Capabilities.Completion, caps.GetProperty("completion").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.Hover, caps.GetProperty("hover").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.SignatureHelp, caps.GetProperty("signatureHelp").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.Definition, caps.GetProperty("definition").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.Navigate, caps.GetProperty("navigate").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.InlayHints, caps.GetProperty("inlayHints").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.CodeLens, caps.GetProperty("codeLens").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.CodeLensClick, caps.GetProperty("codeLensClick").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.SemanticTokens, caps.GetProperty("semanticTokens").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.CompletionResult, caps.GetProperty("completionResult").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.HoverResult, caps.GetProperty("hoverResult").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.SignatureResult, caps.GetProperty("signatureResult").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.DefinitionResult, caps.GetProperty("definitionResult").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.InlayHintsResult, caps.GetProperty("inlayHintsResult").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.CodeLensResult, caps.GetProperty("codeLensResult").GetString());
        Assert.Equal(CideEditorBusManifest.Capabilities.SemanticTokensResult, caps.GetProperty("semanticTokensResult").GetString());
    }

    [Fact]
    public void BusManifestJson_legacy_maps_to_normalizeInbound()
    {
        var legacy = LoadManifestRoot().GetProperty("legacy");
        Assert.Equal(
            CideEditorBusManifest.Capabilities.Completion,
            CideEditorBusManifest.NormalizeInboundType(legacy.GetProperty("requestCompletion").GetString()));
        Assert.Equal(
            CideEditorBusManifest.Capabilities.Hover,
            CideEditorBusManifest.NormalizeInboundType(legacy.GetProperty("requestHover").GetString()));
        Assert.Equal(
            CideEditorBusManifest.Capabilities.SignatureHelp,
            CideEditorBusManifest.NormalizeInboundType(legacy.GetProperty("requestSignature").GetString()));
    }

    [Fact]
    public void Editor_push_types_align_with_bridge_protocol()
    {
        Assert.Equal(CideEditorBridgeTypes.SetModel, CideEditorBusManifest.Editor.SetModel);
        Assert.Equal(CideEditorBridgeTypes.ApplyEdits, CideEditorBusManifest.Editor.ApplyEdits);
        Assert.Equal(CideEditorBridgeTypes.SetDecorations, CideEditorBusManifest.Editor.SetDecorations);
        Assert.Equal(CideEditorBridgeTypes.SetAgentReveal, CideEditorBusManifest.Editor.SetAgentReveal);
        Assert.Equal(CideEditorBridgeTypes.ClearAgentReveal, CideEditorBusManifest.Editor.ClearAgentReveal);
    }

    private static JsonElement LoadManifestRoot()
    {
        var path = Path.Combine(MonacoEditorAssetLocator.GetCideEditorRoot(), "bus-manifest.json");
        Assert.True(File.Exists(path), $"Missing bus manifest: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.Clone();
    }
}
