using System.Text.Json.Serialization;
using CascadeIDE.Services;

namespace CascadeIDE.Models;

/// <summary>Языковые серверы. TOML: <c>[languages.csharp]</c>, <c>[languages.markdown]</c>.</summary>
public sealed class LanguagesSettings
{
    [JsonPropertyName("csharp")]
    public CSharpLanguageServerSettings CSharp { get; set; } = new();

    [JsonPropertyName("markdown")]
    public MarkdownLanguageServerSettings Markdown { get; set; } = new();
}

/// <summary>C# LSP c дискриминатором режима и вложенными профилями запуска.</summary>
public sealed class CSharpLanguageServerSettings
{
    /// <summary>Дискриминатор режима (ParseOnly / OmniSharp / CSharpLs / Custom).</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "ParseOnly";

    [JsonPropertyName("parse_only")]
    public LanguageServerLaunchProfile ParseOnly { get; set; } = new();

    [JsonPropertyName("omni_sharp")]
    public LanguageServerLaunchProfile OmniSharp { get; set; } = new();

    [JsonPropertyName("csharp_ls")]
    public LanguageServerLaunchProfile CSharpLs { get; set; } = new();

    [JsonPropertyName("custom")]
    public LanguageServerLaunchProfile Custom { get; set; } = new();

    public string GetNormalizedMode()
    {
        var mode = Mode;
        if (string.IsNullOrWhiteSpace(mode))
            return "ParseOnly";
        return mode.Trim();
    }

    public (string Mode, string Executable, string Arguments) ResolveForRuntime()
    {
        var mode = GetNormalizedMode();
        var profile = GetProfileForMode(mode);
        return (mode, profile.ResolveExecutable(), profile.ResolveArguments());
    }

    public void SetMode(string mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode) ? "ParseOnly" : mode.Trim();
        Mode = normalized;
    }

    public void SetLaunchOverrides(string mode, string executable, string arguments)
    {
        var profile = GetProfileForMode(mode);
        profile.Executable = executable ?? "";
        profile.Arguments = arguments ?? "";
    }

    private LanguageServerLaunchProfile GetProfileForMode(string mode)
    {
        return mode switch
        {
            "OmniSharp" => OmniSharp,
            "CSharpLs" => CSharpLs,
            "Custom" => Custom,
            _ => ParseOnly,
        };
    }
}

public sealed class LanguageServerLaunchProfile
{
    public string Executable { get; set; } = "";

    /// <summary>TOML: <c>executable_env</c> — <c>PATH</c> (поиск в PATH) или имя переменной с путём (ADR 0149).</summary>
    public string ExecutableEnv { get; set; } = "";

    public string Arguments { get; set; } = "";

    /// <summary>TOML: <c>arguments_env</c>.</summary>
    public string ArgumentsEnv { get; set; } = "";

    public string ResolveExecutable() =>
        SettingsEnvResolver.ResolveLaunchPath(Executable, ExecutableEnv);

    public string ResolveArguments() =>
        SettingsEnvResolver.Resolve(Arguments, ArgumentsEnv);
}

/// <summary>Markdown LSP c дискриминатором режима и вложенными профилями запуска.</summary>
public sealed class MarkdownLanguageServerSettings
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "Off";

    [JsonPropertyName("off")]
    public LanguageServerLaunchProfile Off { get; set; } = new();

    [JsonPropertyName("marksman")]
    public LanguageServerLaunchProfile Marksman { get; set; } = new();

    [JsonPropertyName("custom")]
    public LanguageServerLaunchProfile Custom { get; set; } = new();

    public string GetNormalizedMode()
    {
        var mode = Mode;
        if (string.IsNullOrWhiteSpace(mode))
            return "Off";
        return mode.Trim();
    }

    public (string Mode, string Executable, string Arguments) ResolveForRuntime()
    {
        var mode = GetNormalizedMode();
        var profile = GetProfileForMode(mode);
        return (mode, profile.ResolveExecutable(), profile.ResolveArguments());
    }

    public void SetMode(string mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode) ? "Off" : mode.Trim();
        Mode = normalized;
    }

    public void SetLaunchOverrides(string mode, string executable, string arguments)
    {
        var profile = GetProfileForMode(mode);
        profile.Executable = executable ?? "";
        profile.Arguments = arguments ?? "";
    }

    private LanguageServerLaunchProfile GetProfileForMode(string mode)
    {
        return mode switch
        {
            "Marksman" => Marksman,
            "Custom" => Custom,
            _ => Off,
        };
    }
}
