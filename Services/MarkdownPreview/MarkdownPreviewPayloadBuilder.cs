using Markdig;
using CascadeIDE.Models;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Единая точка include/diagram expansion и Markdig parse перед рендером.</summary>
public sealed class MarkdownPreviewPayloadBuilder
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public async Task<MarkdownPreviewPayload> BuildAsync(
        MarkdownPreviewSource source,
        CascadeIdeSettings settings,
        CancellationToken cancellationToken = default)
    {
        var title = string.IsNullOrWhiteSpace(source.Title) ? "Markdown Preview" : source.Title.Trim();
        var raw = source.Markdown ?? "";
        var renderMarkdown = raw;
        var notices = new List<string>();

        if (!string.IsNullOrWhiteSpace(source.SourcePath))
        {
            try
            {
                renderMarkdown = MarkdownIncludeExpansion.ExpandMarkdown(renderMarkdown, source.SourcePath!);
            }
            catch (IncludeExpansionException ex)
            {
                notices.Add($"Include expansion: {ex.Message}");
            }
        }

        try
        {
            renderMarkdown = await MarkdownDiagramExpansion.ExpandAsync(renderMarkdown, settings, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            notices.Add($"Diagram expansion: {ex.Message}");
        }

        try
        {
            var document = Markdig.Markdown.Parse(renderMarkdown, Pipeline);
            return new MarkdownPreviewPayload(title, raw, renderMarkdown, source.SourcePath, document, notices, null);
        }
        catch (Exception ex)
        {
            notices.Add("Rendered as fallback because Markdown parsing failed.");
            return new MarkdownPreviewPayload(title, raw, renderMarkdown, source.SourcePath, null, notices, ex.Message);
        }
    }
}
