using CascadeIDE.Services.MarkdownPreview;
using Markdig;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MarkdownPreviewNavigationTests
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    [Theory]
    [InlineData("doc.md#section", "doc.md", "section")]
    [InlineData("#only", null, "only")]
    [InlineData("https://example.com", "https://example.com", null)]
    public void SplitUrl_ParsesPathAndFragment(string url, string? expectedPath, string? expectedFragment)
    {
        var (path, fragment) = MarkdownPreviewRenderContext.SplitUrl(url);
        Assert.Equal(expectedPath, path);
        Assert.Equal(expectedFragment, fragment);
    }

    [Fact]
    public void HeadingSlug_NormalizesText()
    {
        MarkdownPreviewHeadingSlug.ResetSlugCounts();
        var slug = MarkdownPreviewHeadingSlug.Create("## Связанные ADR (v2)");
        Assert.Equal("связанные-adr-v2", slug);
    }

    [Fact]
    public void ExtractHtmlAnchorIds_FindsId()
    {
        var ids = MarkdownPreviewHeadingSlug.ExtractHtmlAnchorIds("""<a id="foo-bar"></a>""").ToArray();
        Assert.Single(ids);
        Assert.Equal("foo-bar", ids[0]);
    }

    [Fact]
    public void Render_StrikethroughAndTaskList_DoesNotThrow()
    {
        const string md = """
            ~~removed~~

            - [x] done
            - [ ] todo
            """;
        var doc = Markdown.Parse(md, Pipeline);
        var payload = new MarkdownPreviewPayload("t.md", md, md, null, doc, [], null);
        var control = new MarkdigMarkdownPreviewRenderer().Render(payload);
        Assert.NotNull(control);
    }

    [Fact]
    public void Render_HtmlAnchorBlock_DoesNotThrow()
    {
        const string md = """<a id="anchor-id"></a>""";
        var doc = Markdown.Parse(md, Pipeline);
        var payload = new MarkdownPreviewPayload("t.md", md, md, null, doc, [], null);
        var registry = new MarkdownPreviewAnchorRegistry();
        var ctx = new MarkdownPreviewRenderContext(null, null, anchors: registry);
        var control = new MarkdigMarkdownPreviewRenderer().Render(payload, ctx);
        Assert.NotNull(control);
    }
}
