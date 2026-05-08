using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;
using TestContext = Xunit.TestContext;

namespace CascadeIDE.Tests;

public class MarkdownDiagramExpansionTests
{
    [Fact]
    public async Task ExpandAsync_KrokiDisabled_LeavesMermaidFenceUntouched()
    {
        var md = "```mermaid\ngraph LR\n  A-->B\n```";
        var settings = new CascadeIdeSettings
        {
            Markdown = new MarkdownSettings { Diagrams = new MarkdownDiagramSettings { Kroki = false } }
        };
        var result = await MarkdownDiagramExpansion.ExpandAsync(md, settings, TestContext.Current.CancellationToken);
        Assert.Equal(md, result);
    }

    [Fact]
    public async Task ExpandAsync_NoDiagramFences_ReturnsUnchangedWithoutNetwork()
    {
        var md = "# Title\n\n```csharp\nvar x = 1;\n```\n";
        var settings = new CascadeIdeSettings
        {
            Markdown = new MarkdownSettings
            {
                Diagrams = new MarkdownDiagramSettings { Kroki = true, KrokiUrl = "https://kroki.io" }
            }
        };
        var result = await MarkdownDiagramExpansion.ExpandAsync(md, settings, TestContext.Current.CancellationToken);
        Assert.Equal(md, result);
    }
}
