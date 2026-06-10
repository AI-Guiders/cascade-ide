using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>Forge Lens Bearer tokens (не коммитить). ADR 0158 §3.</summary>
public static class ForgeLensSecretsStorage
{
    private static string GetPath() =>
        Path.Combine(SettingsService.GetSettingsDirectory(), "forge-lens-secrets.toml");

    public static ForgeLensSecrets Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return new ForgeLensSecrets();
            var toml = File.ReadAllText(path);
            return CascadeTomlSerializer.Deserialize<ForgeLensSecrets>(toml) ?? new ForgeLensSecrets();
        }
        catch
        {
            return new ForgeLensSecrets();
        }
    }

    public static void Save(ForgeLensSecrets secrets)
    {
        try
        {
            var toml = CascadeTomlSerializer.Serialize(secrets);
            File.WriteAllText(GetPath(), toml);
        }
        catch
        {
            // best-effort
        }
    }

    public static void SetToken(string baseUrl, string apiToken, string? tokenName = null)
    {
        var key = ForgeLensHostUrl.NormalizeKey(baseUrl);
        var secrets = Load();
        secrets.Hosts[key] = new ForgeLensHostSecrets
        {
            ApiToken = apiToken.Trim(),
            TokenName = tokenName?.Trim() ?? "",
            SavedAtUtc = DateTimeOffset.UtcNow.ToString("O"),
        };
        Save(secrets);
    }

    public static bool RemoveHost(string baseUrl)
    {
        var key = ForgeLensHostUrl.NormalizeKey(baseUrl);
        var secrets = Load();
        if (!secrets.Hosts.Remove(key))
            return false;
        Save(secrets);
        return true;
    }

    public static string? TryGetToken(string baseUrl)
    {
        var key = ForgeLensHostUrl.NormalizeKey(baseUrl);
        var secrets = Load();
        return secrets.Hosts.TryGetValue(key, out var entry) && entry.HasApiToken
            ? entry.ApiToken
            : null;
    }
}
