using CascadeIDE.Models;

namespace CascadeIDE.Services;

public static class SettingsService
{
    public static string GetSettingsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "CascadeIDE");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetSettingsPath() => Path.Combine(GetSettingsDirectory(), "settings.toml");

    public static CascadeIdeSettings Load()
    {
        try
        {
            var tomlPath = GetSettingsPath();
            if (!File.Exists(tomlPath))
                return new CascadeIdeSettings();

            var toml = File.ReadAllText(tomlPath);
            var settings = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(toml) ?? new CascadeIdeSettings();
            SettingsTomlMigration.ApplyLegacyFlatKeys(settings, toml);
            return settings;
        }
        catch
        {
            return new CascadeIdeSettings();
        }
    }

    public static void Save(CascadeIdeSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            var toml = CascadeTomlSerializer.Serialize(settings);
            File.WriteAllText(path, toml);
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }
}
