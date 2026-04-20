namespace CascadeIDE.Models;

/// <summary>Стабильные id ячеек deck экрана «готовность окружения» (ADR 0063; не пользовательский TOML).</summary>
public static class EnvironmentReadinessCellIds
{
    /// <summary>Сводная лампа блока «Dev Tools» (мост агента и далее по строкам): Ok, если по деталям нет Caution/Critical.</summary>
    public const string DevToolsSection = "environment_dev_tools_section";
    public const string Agent = "environment_agent";
    /// <summary>Сводная лампа блока AGENT_NOTES_* / NETCOREDBG_PATH: Ok, если нет Caution/Critical по детальным строкам.</summary>
    public const string EnvSection = "environment_env_section";
    public const string AgentNotesFile = "environment_agent_notes_file";
    public const string AgentNotesCanonPath = "environment_agent_notes_canon_path";
    public const string NetcoreDbgPath = "environment_netcoredbg_path";
    public const string CSharpLsp = "environment_csharp_lsp";
    public const string MarkdownLsp = "environment_markdown_lsp";
    public const string DotnetSdk = "environment_dotnet_sdk";
}
