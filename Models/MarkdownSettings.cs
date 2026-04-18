namespace CascadeIDE.Models;

/// <summary>Markdown в IDE: диаграммы Kroki и т.д. TOML: <c>[markdown.diagrams]</c>.</summary>
public sealed class MarkdownSettings
{
    public MarkdownDiagramSettings Diagrams { get; set; } = new();
}
