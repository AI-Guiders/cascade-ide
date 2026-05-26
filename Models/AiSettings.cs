using System.Text.Json.Serialization;
using CascadeIDE.Services;

namespace CascadeIDE.Models;

/// <summary>Контур AI в <c>[ai]</c> (<c>settings.toml</c>). ADR 0083: <c>mode</c> и вложенные таблицы.</summary>
public sealed class AiSettings
{
    /// <summary>Один из: <c>local</c>, <c>acp</c>, <c>mcp_only</c>, <c>cloud</c>.</summary>
    public string Mode { get; set; } = "local";

    public AiLocalSettings Local { get; set; } = new();

    public AiAcpSettings Acp { get; set; } = new();

    public AiMcpOnlySettings McpOnly { get; set; } = new();

    public AiCloudSettings Cloud { get; set; } = new();

    public AiChatSettings Chat { get; set; } = new();

    /// <summary>Ключ провайдера для существующего кода чата (Ollama, Anthropic, …).</summary>
    public string ResolveEffectiveProviderUiKey()
    {
        return NormalizeMode(Mode) switch
        {
            "local" => "Ollama",
            "acp" => "CursorACP",
            "mcp_only" => "Ollama",
            "cloud" => NormalizeCloudProvider(Cloud.ActiveProvider) switch
            {
                "anthropic" => "Anthropic",
                "openai" => "OpenAI",
                "deepseek" => "DeepSeek",
                _ => "Anthropic"
            },
            _ => "Ollama"
        };
    }

    public static string NormalizeMode(string? mode)
    {
        var m = (mode ?? "local").Trim().ToLowerInvariant();
        return m is "local" or "acp" or "mcp_only" or "cloud" ? m : "local";
    }

    public static string NormalizeCloudProvider(string? p)
    {
        var x = (p ?? "anthropic").Trim().ToLowerInvariant();
        return x is "anthropic" or "openai" or "deepseek" ? x : "anthropic";
    }
}

/// <summary>TOML: <c>[ai.local]</c> и <c>[ai.local.ollama]</c>.</summary>
public sealed class AiLocalSettings
{
    /// <summary>Например <c>ollama</c>; должен совпадать с активной подтаблицей.</summary>
    public string Backend { get; set; } = "ollama";

    public AiLocalOllamaSettings Ollama { get; set; } = new();
}

/// <summary>TOML: <c>[ai.local.ollama]</c>.</summary>
public sealed class AiLocalOllamaSettings
{
    public string Model { get; set; } = "qwen2.5-coder:7b";
}

/// <summary>TOML: <c>[ai.acp]</c>.</summary>
public sealed class AiAcpSettings
{
    public string CursorAcpPath { get; set; } = "";

    /// <summary>TOML: <c>cursor_acp_path_env</c> — <c>PATH</c> или имя переменной с путём (ADR 0149).</summary>
    public string CursorAcpPathEnv { get; set; } = "";

    public string CursorAcpModelId { get; set; } = "";

    public string ResolveCursorAcpPath() =>
        SettingsEnvResolver.ResolveLaunchPath(CursorAcpPath, CursorAcpPathEnv);
}

/// <summary>TOML: <c>[ai.mcp_only]</c> — зарезервировано под флаги режима.</summary>
public sealed class AiMcpOnlySettings
{
}

/// <summary>TOML: <c>[ai.cloud]</c> и <c>[ai.cloud.*]</c>.</summary>
public sealed class AiCloudSettings
{
    public string ActiveProvider { get; set; } = "anthropic";

    [JsonPropertyName("anthropic")]
    public AiCloudAnthropicSettings Anthropic { get; set; } = new();

    [JsonPropertyName("openai")]
    public AiCloudOpenAiSettings OpenAi { get; set; } = new();

    [JsonPropertyName("deepseek")]
    public AiCloudDeepSeekSettings DeepSeek { get; set; } = new();
}

public sealed class AiCloudAnthropicSettings
{
    public string Model { get; set; } = "claude-sonnet-4-20250514";
}

public sealed class AiCloudOpenAiSettings
{
    public string BaseUrl { get; set; } = "https://api.openai.com";

    public string Model { get; set; } = "gpt-4o";
}

public sealed class AiCloudDeepSeekSettings
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com";

    public string Model { get; set; } = "deepseek-chat";
}

/// <summary>TOML: <c>[ai.chat]</c>.</summary>
public sealed class AiChatSettings
{
    /// <summary><c>mfd</c> | <c>window</c>.</summary>
    public string SettingsPresentation { get; set; } = "mfd";

    public bool ShowThinkingInHistory { get; set; } = true;
}
