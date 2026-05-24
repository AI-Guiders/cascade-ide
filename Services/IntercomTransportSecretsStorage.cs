using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>JWT refresh/access для Intercom transport (не коммитить).</summary>
public static class IntercomTransportSecretsStorage
{
    private static string GetPath() =>
        Path.Combine(SettingsService.GetSettingsDirectory(), "intercom-transport-secrets.toml");

    public static IntercomTransportSecrets Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return new IntercomTransportSecrets();
            var toml = File.ReadAllText(path);
            return CascadeTomlSerializer.Deserialize<IntercomTransportSecrets>(toml) ?? new IntercomTransportSecrets();
        }
        catch
        {
            return new IntercomTransportSecrets();
        }
    }

    public static void Save(IntercomTransportSecrets secrets)
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
}
