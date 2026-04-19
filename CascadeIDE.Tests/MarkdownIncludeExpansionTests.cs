using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MarkdownIncludeExpansionTests
{
    [Fact]
    public void ExpandMarkdown_ExpandsIncludeInsideFence()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascadeide-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var md = Path.Combine(dir, "a.md");
        var diagram = Path.Combine(dir, "d.mmd");
        File.WriteAllText(diagram, "graph TD;\nA-->B;\n");

        var src = """
                 # Doc

                 ```mermaid
                 {{ INCLUDE: d.mmd }}
                 ```
                 """;

        var expanded = MarkdownIncludeExpansion.ExpandMarkdown(src, md);
        Assert.Contains("graph TD;", expanded);
        Assert.DoesNotContain("{{ INCLUDE:", expanded);
    }

    [Fact]
    public void ExpandMarkdown_DoesNotExpandOutsideFence()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cascadeide-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var md = Path.Combine(dir, "a.md");
        var inc = Path.Combine(dir, "x.txt");
        File.WriteAllText(inc, "hello");

        var src = "{{ INCLUDE: x.txt }}\n";
        var expanded = MarkdownIncludeExpansion.ExpandMarkdown(src, md);
        Assert.Contains("{{ INCLUDE: x.txt }}", expanded);
    }
}

