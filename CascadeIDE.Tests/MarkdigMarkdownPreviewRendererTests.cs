using Avalonia.Controls;
using CascadeIDE.Services.MarkdownPreview;
using Markdig;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MarkdigMarkdownPreviewRendererTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    [Fact]
    public void Render_AdrRelatedTable_DoesNotThrow()
    {
        const string md = """
            ## Связанные ADR

            | ADR | Роль |
            |-----|------|
            | [0155](0155-documentation-code-correspondence-and-architectural-drift.md) | Слои L0–L4 |
            | [0061](0061-context-aware-adr-map-pfd-knowledge-indicator.md) | L1 map |
            """;

        var doc = Markdown.Parse(md, Pipeline);
        var payload = new MarkdownPreviewPayload(
            "docs/adr/0156-test.md",
            md,
            md,
            "docs/adr/0156-test.md",
            doc,
            [],
            null);
        var r = new MarkdigMarkdownPreviewRenderer();
        var control = r.Render(payload, new MarkdownPreviewRenderContext("docs/adr/0156-test.md", null));
        Assert.NotNull(control);
        Assert.IsType<ScrollViewer>(control);
    }

    [Fact]
    public void Render_DoesNotThrow_OnTypicalDocument()
    {
        const string md = """
            # Title

            **bold** and *italic* and `code`.

            | A | B |
            |---|---|
            | 1 | 2 |

            ```text
            fenced
            ```
            """;
        var doc = Markdown.Parse(md, Pipeline);
        var payload = new MarkdownPreviewPayload(
            "t.md",
            md,
            md,
            null,
            doc,
            [],
            null);
        var r = new MarkdigMarkdownPreviewRenderer();
        var control = r.Render(payload);
        Assert.NotNull(control);
        Assert.IsType<ScrollViewer>(control);
    }

    [Fact]
    public void Render_DoesNotStackOverflow_OnMarkdownLink()
    {
        const string md = "See [docs](https://example.com) and ![alt](img.png).";
        var doc = Markdown.Parse(md, Pipeline);
        var payload = new MarkdownPreviewPayload(
            "link.md",
            md,
            md,
            null,
            doc,
            [],
            null);
        var r = new MarkdigMarkdownPreviewRenderer();
        var control = r.Render(payload);
        Assert.NotNull(control);
    }

    [Fact]
    public void Render_CodeAnchorPreviewLink_DoesNotThrow()
    {
        const string md = "Impl [F:Features/Chat/Foo.cs M:RunAsync].";
        var expanded = MarkdownCodeAnchorPreviewExpander.Expand(md);
        var doc = Markdown.Parse(expanded, Pipeline);
        var payload = new MarkdownPreviewPayload(
            "anchor.md",
            md,
            expanded,
            null,
            doc,
            [],
            null);
        var r = new MarkdigMarkdownPreviewRenderer();
        var ctx = new MarkdownPreviewRenderContext(null, null, _ => { });
        var control = r.Render(payload, ctx);
        Assert.NotNull(control);
    }

    [Fact]
    public void Render_Fallback_OnNullDocument_StillReturnsControl()
    {
        var payload = new MarkdownPreviewPayload("x", "", "", null, null, [], "parse error");
        var r = new MarkdigMarkdownPreviewRenderer();
        var control = r.Render(payload);
        Assert.NotNull(control);
    }
}
