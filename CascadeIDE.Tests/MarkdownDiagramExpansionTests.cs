using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public class MarkdownDiagramExpansionTests
{
    [Fact]
    public async Task ExpandAsync_KrokiDisabled_LeavesMermaidFenceUntouched()
    {
        var md = "```mermaid\ngraph LR\n  A-->B\n```";
        var settings = new CascadeIdeSettings { MarkdownKrokiEnabled = false };
        var result = await MarkdownDiagramExpansion.ExpandAsync(md, settings);
        Assert.Equal(md, result);
    }

    [Fact]
    public async Task ExpandAsync_NoDiagramFences_ReturnsUnchangedWithoutNetwork()
    {
        var md = "# Title\n\n```csharp\nvar x = 1;\n```\n";
        var settings = new CascadeIdeSettings { MarkdownKrokiEnabled = true, MarkdownKrokiBaseUrl = "https://kroki.io" };
        var result = await MarkdownDiagramExpansion.ExpandAsync(md, settings);
        Assert.Equal(md, result);
    }
}
