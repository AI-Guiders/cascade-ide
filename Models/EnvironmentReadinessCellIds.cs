namespace CascadeIDE.Models;

/// <summary>Стабильные id ячеек deck экрана «готовность окружения» (ADR 0063; не пользовательский TOML).</summary>
public static class EnvironmentReadinessCellIds
{
    public const string Agent = "environment_agent";
    public const string CSharpLsp = "environment_csharp_lsp";
    public const string MarkdownLsp = "environment_markdown_lsp";
    public const string DotnetSdk = "environment_dotnet_sdk";
}
