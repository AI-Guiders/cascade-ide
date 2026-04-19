using Avalonia.Controls;

namespace CascadeIDE.Services.MarkdownPreview;

/// <summary>Renderer preview payload -> Avalonia control tree.</summary>
public interface IMarkdownPreviewRenderer
{
    Control Render(MarkdownPreviewPayload payload);
}
