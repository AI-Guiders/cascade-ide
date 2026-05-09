using CascadeIDE.Contracts;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Markdown.Application;

/// <summary>Развернуть include в Markdown и записать файл (без UI).</summary>
[ComputingUnit("markdown-expand-export")]
public static class ExpandedMarkdownFileExportProjection
{
    public readonly record struct WriteOutcome(bool Ok, string? ErrorMessage);

    public static WriteOutcome TryExpandAndWriteAllText(string sourceMarkdownPath, string rawMarkdown, string outputPath)
    {
        try
        {
            var expanded = MarkdownIncludeExpansion.ExpandMarkdown(rawMarkdown, sourceMarkdownPath);
            File.WriteAllText(outputPath, expanded);
            return new WriteOutcome(true, null);
        }
        catch (IncludeExpansionException iex)
        {
            return new WriteOutcome(false, iex.Message);
        }
        catch (Exception ex)
        {
            return new WriteOutcome(false, ex.Message);
        }
    }
}
