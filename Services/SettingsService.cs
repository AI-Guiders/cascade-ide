using CascadeIDE.Models;
using Tomlyn;

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
            var dir = GetSettingsDirectory();
            var tomlPath = GetSettingsPath();
            var jsonPath = Path.Combine(dir, "settings.json");

            if (File.Exists(tomlPath))
            {
                var toml = File.ReadAllText(tomlPath);
                return Toml.ToModel<CascadeIdeSettings>(toml) ?? new CascadeIdeSettings();
            }

            // Однократная миграция с JSON на TOML
            if (File.Exists(jsonPath))
            {
                try
                {
                    var json = File.ReadAllText(jsonPath);
                    var migrated = System.Text.Json.JsonSerializer.Deserialize<CascadeIdeSettings>(json) ?? new CascadeIdeSettings();
                    Save(migrated);
                    File.Delete(jsonPath);
                    return migrated;
                }
                catch
                {
                    return new CascadeIdeSettings();
                }
            }

            return new CascadeIdeSettings();
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
            var toml = Toml.FromModel(settings);
            File.WriteAllText(path, toml);
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }
}
