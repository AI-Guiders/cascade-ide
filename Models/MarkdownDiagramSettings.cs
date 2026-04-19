namespace CascadeIDE.Models;

/// <summary>Kroki и превью диаграмм в Markdown.</summary>
public sealed class MarkdownDiagramSettings
{
    public bool Kroki { get; set; } = true;

    public string KrokiUrl { get; set; } = "https://kroki.io";
}
