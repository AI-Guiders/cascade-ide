namespace CascadeIDE.Models;

/// <summary>Markdown LSP (<c>[markdown_lsp]</c> в <c>settings.toml</c>).</summary>
public sealed class MarkdownLspSettings
{
    /// <summary>Off, Marksman, Custom.</summary>
    public string Provider { get; set; } = "Off";

    public string Executable { get; set; } = "";

    public string Arguments { get; set; } = "";
}
