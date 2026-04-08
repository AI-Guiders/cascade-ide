using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>Чтение/запись API-ключей в %LocalAppData%\CascadeIDE\ai-keys.toml (не коммитить).</summary>
public static class AiKeysStorage
{
    private static string GetPath() => Path.Combine(SettingsService.GetSettingsDirectory(), "ai-keys.toml");

    public static AiKeys Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return new AiKeys();
            var toml = File.ReadAllText(path);
            return CascadeTomlSerializer.Deserialize<AiKeys>(toml) ?? new AiKeys();
        }
        catch
        {
            return new AiKeys();
        }
    }

    public static void Save(AiKeys keys)
    {
        try
        {
            var toml = CascadeTomlSerializer.Serialize(keys);
            File.WriteAllText(GetPath(), toml);
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }
}
