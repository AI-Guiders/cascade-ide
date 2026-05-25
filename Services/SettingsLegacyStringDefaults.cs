using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Подставляет заводские строковые дефолты, если в пользовательском TOML поле было явно пустым
/// (merge не перезаписывает дефолт, но <c>""</c> из старого файла — да).
/// </summary>
public static class SettingsLegacyStringDefaults
{
    public static void Apply(CascadeIdeSettings settings)
    {
        ApplyIntercomTransport(settings.Intercom.Transport);
        ApplyHybridIndex(settings.HybridIndex);
        ApplyWorkspace(settings.Workspace);
        ApplyLanguages(settings.Languages);
        ApplyAi(settings.Ai);
        ApplyMcp(settings.Mcp);
        ApplyAgentNotes(settings.AgentNotes);
        ApplyAgentEnvironment(settings.Agent.Environment);
        ApplyCodeNavigationMap(settings.CodeNavigationMap);
        ApplyMarkdown(settings.Markdown);
    }

    private static void ApplyIntercomTransport(IntercomTransportSettings t)
    {
        if (string.IsNullOrWhiteSpace(t.BaseUrl))
            t.BaseUrl = IntercomTransportSettings.DefaultBaseUrl;
        if (string.IsNullOrWhiteSpace(t.LocalServerPath))
            t.LocalServerPath = IntercomTransportSettings.DefaultLocalServerRelativePath;
        if (string.IsNullOrWhiteSpace(t.OAuthProvider))
            t.OAuthProvider = "github";
    }

    private static void ApplyHybridIndex(HybridIndexSettings h)
    {
        if (string.IsNullOrWhiteSpace(h.IndexDir))
            h.IndexDir = ".hybrid-codebase-index";
        if (string.IsNullOrWhiteSpace(h.ScopeMode))
            h.ScopeMode = "workspace+solution";
    }

    private static void ApplyWorkspace(WorkspaceSettings w)
    {
        if (string.IsNullOrWhiteSpace(w.Mode))
            w.Mode = "Flight";
        if (string.IsNullOrWhiteSpace(w.PrimaryWorkSurface))
            w.PrimaryWorkSurface = PrimaryWorkSurfaceKindExtensions.EditorValue;
    }

    private static void ApplyLanguages(LanguagesSettings l)
    {
        if (string.IsNullOrWhiteSpace(l.CSharp.Mode))
            l.CSharp.Mode = "OmniSharp";
        if (string.IsNullOrWhiteSpace(l.Markdown.Mode))
            l.Markdown.Mode = "Marksman";
    }

    private static void ApplyAi(AiSettings ai)
    {
        if (string.IsNullOrWhiteSpace(ai.Mode))
            ai.Mode = "local";
        if (string.IsNullOrWhiteSpace(ai.Local.Backend))
            ai.Local.Backend = "ollama";
        if (string.IsNullOrWhiteSpace(ai.Local.Ollama.Model))
            ai.Local.Ollama.Model = "qwen2.5-coder:7b";
        if (string.IsNullOrWhiteSpace(ai.Cloud.ActiveProvider))
            ai.Cloud.ActiveProvider = "anthropic";
        if (string.IsNullOrWhiteSpace(ai.Cloud.Anthropic.Model))
            ai.Cloud.Anthropic.Model = "claude-sonnet-4-20250514";
        if (string.IsNullOrWhiteSpace(ai.Cloud.OpenAi.BaseUrl))
            ai.Cloud.OpenAi.BaseUrl = "https://api.openai.com";
        if (string.IsNullOrWhiteSpace(ai.Cloud.OpenAi.Model))
            ai.Cloud.OpenAi.Model = "gpt-4o";
        if (string.IsNullOrWhiteSpace(ai.Cloud.DeepSeek.BaseUrl))
            ai.Cloud.DeepSeek.BaseUrl = "https://api.deepseek.com";
        if (string.IsNullOrWhiteSpace(ai.Cloud.DeepSeek.Model))
            ai.Cloud.DeepSeek.Model = "deepseek-chat";
        if (string.IsNullOrWhiteSpace(ai.Chat.SettingsPresentation))
            ai.Chat.SettingsPresentation = "mfd";
    }

    private static void ApplyMcp(McpSettings m)
    {
        if (string.IsNullOrWhiteSpace(m.ExternalServersJson))
            m.ExternalServersJson = "[]";
    }

    private static void ApplyAgentNotes(AgentNotesSettings n)
    {
        // Пустые config_path / kb_base_overlay_path — намеренно (встроенный KB).
    }

    private static void ApplyAgentEnvironment(AgentEnvironmentSettings e)
    {
        if (string.IsNullOrWhiteSpace(e.DefaultVerifyPolicy))
            e.DefaultVerifyPolicy = "standard";
        if (string.IsNullOrWhiteSpace(e.DefaultSandboxProfile))
            e.DefaultSandboxProfile = "agent_ephemeral";
        if (string.IsNullOrWhiteSpace(e.ShellEscapeTier))
            e.ShellEscapeTier = "deny";
    }

    private static void ApplyCodeNavigationMap(CodeNavigationMapSettings m)
    {
        if (string.IsNullOrWhiteSpace(m.View))
            m.View = "list";
        if (string.IsNullOrWhiteSpace(m.Depth))
            m.Depth = CodeNavigationMapLevelKind.File;
        if (string.IsNullOrWhiteSpace(m.DetailLevel))
            m.DetailLevel = "normal";
    }

    private static void ApplyMarkdown(MarkdownSettings md)
    {
        if (string.IsNullOrWhiteSpace(md.Diagrams.KrokiUrl))
            md.Diagrams.KrokiUrl = "https://kroki.io";
    }
}
