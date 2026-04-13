namespace CascadeIDE.Models;

/// <summary>C# language server (<c>[csharp_lsp]</c> в <c>settings.toml</c>).</summary>
public sealed class CSharpLspSettings
{
    /// <summary>ParseOnly, OmniSharp, CSharpLs, Custom.</summary>
    public string Provider { get; set; } = "ParseOnly";

    public string Executable { get; set; } = "";

    public string Arguments { get; set; } = "";
}
