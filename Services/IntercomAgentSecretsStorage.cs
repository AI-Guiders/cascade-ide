using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>Agent API tokens для Intercom (не коммитить).</summary>
public static class IntercomAgentSecretsStorage
{
    private static string GetPath() =>
        Path.Combine(SettingsService.GetSettingsDirectory(), "intercom-agent-secrets.toml");

    public static IntercomAgentSecrets Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return new IntercomAgentSecrets();
            var toml = File.ReadAllText(path);
            return CascadeTomlSerializer.Deserialize<IntercomAgentSecrets>(toml) ?? new IntercomAgentSecrets();
        }
        catch
        {
            return new IntercomAgentSecrets();
        }
    }

    public static void Save(IntercomAgentSecrets secrets)
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
