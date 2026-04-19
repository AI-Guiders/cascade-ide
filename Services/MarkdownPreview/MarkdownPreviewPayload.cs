using Markdig.Syntax;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Подготовленный payload для render-layer Markdown preview.</summary>
public sealed record MarkdownPreviewPayload(
    string Title,
    string RawMarkdown,
    string RenderMarkdown,
    string? SourcePath,
    MarkdownDocument? Document,
    IReadOnlyList<string> Notices,
    string? ErrorMessage);
