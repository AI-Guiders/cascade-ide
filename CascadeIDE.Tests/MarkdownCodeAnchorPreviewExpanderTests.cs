using CascadeIDE.Services.MarkdownPreview;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MarkdownCodeAnchorPreviewExpanderTests
{
    [Fact]
    public void Expand_turns_bracket_anchor_into_markdown_link()
    {
        const string md = "See [F:Features/Chat/Foo.cs M:RunAsync] in prose.";
        var expanded = MarkdownCodeAnchorPreviewExpander.Expand(md);
        Assert.Contains("](cascade-code-anchor:", expanded, StringComparison.Ordinal);
        Assert.Contains("RunAsync", expanded, StringComparison.Ordinal);
        Assert.DoesNotContain("[F:Features/Chat/Foo.cs M:RunAsync]", expanded, StringComparison.Ordinal);
    }

    [Fact]
    public void Expand_skips_fenced_code_and_markdown_links()
    {
        const string md = """
            [ADR](docs/adr/0001.md)
            ```text
            [F:Inside/Fence.cs M:No]
            ```
            """;
        var expanded = MarkdownCodeAnchorPreviewExpander.Expand(md);
        Assert.Contains("[ADR](docs/adr/0001.md)", expanded, StringComparison.Ordinal);
        Assert.Contains("[F:Inside/Fence.cs M:No]", expanded, StringComparison.Ordinal);
    }
}
