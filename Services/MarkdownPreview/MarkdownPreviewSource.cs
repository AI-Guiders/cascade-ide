namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Источник данных для построения preview payload.</summary>
public sealed record MarkdownPreviewSource(
    string Title,
    string Markdown,
    string? SourcePath = null);
