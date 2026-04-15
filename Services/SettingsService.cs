using CascadeIDE.Models;

namespace CascadeIDE.Services;

public static class SettingsService
{
    private static readonly ISettingsValidationSpecification[] ValidationSpecifications =
    [
        new DisplaySettingsValidationSpecification()
    ];

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
                return ValidateAndReturn(new CascadeIdeSettings());

            var toml = File.ReadAllText(tomlPath);
            var settings = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(toml) ?? new CascadeIdeSettings();
            return ValidateAndReturn(settings);
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

    private static CascadeIdeSettings ValidateAndReturn(CascadeIdeSettings settings)
    {
        foreach (var validationError in ValidationSpecifications.SelectMany(spec => spec.Validate(settings)))
            global::System.Diagnostics.Debug.WriteLine($"Settings validation: {validationError}");
        return settings;
    }
}
