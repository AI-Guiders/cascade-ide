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
    public void Render_Fallback_OnNullDocument_StillReturnsControl()
    {
        var payload = new MarkdownPreviewPayload("x", "", "", null, null, [], "parse error");
        var r = new MarkdigMarkdownPreviewRenderer();
        var control = r.Render(payload);
        Assert.NotNull(control);
    }
}
