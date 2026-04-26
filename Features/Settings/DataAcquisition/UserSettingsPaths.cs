namespace CascadeIDE.Features.Settings.DataAcquisition;

/// <summary>DAL: каталог и пути к файлам настроек в %LocalAppData%\CascadeIDE\.</summary>
public static class UserSettingsPaths
{
    public static string GetSettingsDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "CascadeIDE");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        return dir;
    }

    public static string GetSettingsFilePath() => Path.Combine(GetSettingsDirectory(), "settings.toml");

    public static string GetHotkeysUserFilePath() =>
        Path.Combine(GetSettingsDirectory(), "hotkeys.toml");

    public static string GetBundledHotkeysFilePath() => Path.Combine(AppContext.BaseDirectory, "Hotkeys", "hotkeys.toml");
}
