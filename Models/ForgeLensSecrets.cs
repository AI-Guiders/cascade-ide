namespace CascadeIDE.Models;

/// <summary>API tokens Forge Lens per forge host. Файл: forge-lens-secrets.toml (ADR 0028, 0158).</summary>
public sealed class ForgeLensSecrets
{
    public Dictionary<string, ForgeLensHostSecrets> Hosts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ForgeLensHostSecrets
{
    public string ApiToken { get; set; } = "";

    public string TokenName { get; set; } = "";

    public string SavedAtUtc { get; set; } = "";

    public bool HasApiToken => !string.IsNullOrWhiteSpace(ApiToken);
}
