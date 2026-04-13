namespace CascadeIDE.Models;

/// <summary>Kroki и превью диаграмм в Markdown (<c>[markdown_diagrams]</c> в <c>settings.toml</c>).</summary>
public sealed class MarkdownDiagramSettings
{
    public bool KrokiEnabled { get; set; } = true;

    public string KrokiBaseUrl { get; set; } = "https://kroki.io";
}
