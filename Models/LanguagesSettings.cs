namespace CascadeIDE.Models;

/// <summary>Языковые серверы. TOML: <c>[languages.csharp]</c>, <c>[languages.markdown]</c>.</summary>
public sealed class LanguagesSettings
{
    public LanguageServerProfile CSharp { get; set; } = new()
    {
        Provider = "ParseOnly"
    };

    public LanguageServerProfile Markdown { get; set; } = new()
    {
        Provider = "Off"
    };
}

/// <summary>Профиль запуска LSP (C# / Markdown).</summary>
public sealed class LanguageServerProfile
{
    public string Provider { get; set; } = "";

    public string Executable { get; set; } = "";

    public string Arguments { get; set; } = "";
}
