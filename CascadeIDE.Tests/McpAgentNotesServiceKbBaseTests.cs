using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class McpAgentNotesServiceKbBaseTests
{
    [Fact]
    public void ReadKnowledgeFile_LoadsEmbeddedSmoke_WhenRuntimeNotConfigured()
    {
        AgentNotesRuntimeLoader.Reset();
        using var stream = typeof(KbBaseEmbeddedBundleProvisioner).Assembly.GetManifestResourceStream(
            KbBaseEmbeddedBundleProvisioner.ResourceName);
        if (stream is null)
            return; // KbBase/kb-base-cide.zip optional in checkout — см. tools/publish-kb-base-embed.ps1

        var embeddedRoot = KbBaseEmbeddedBundleProvisioner.TryGetEmbeddedCanonRoot();
        if (embeddedRoot is null)
            return;

        var smokePath = Path.Combine(embeddedRoot, "knowledge", "kb-base-cide-smoke.md");
        if (!File.Exists(smokePath))
            return; // zip без smoke-файла в этой сборке

        var svc = new McpAgentNotesService(() => new CascadeIdeSettings());
        var text = svc.ReadKnowledgeFile("kb-base-cide-smoke.md").Trim();

        Assert.Equal("kb-base-cide-smoke", text);
    }
}
