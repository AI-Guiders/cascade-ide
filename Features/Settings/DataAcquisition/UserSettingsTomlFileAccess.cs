namespace CascadeIDE.Features.Settings.DataAcquisition;

/// <summary>DAL: сырой I/O для <c>settings.toml</c> в каталоге пользователя.</summary>
public static class UserSettingsTomlFileAccess
{
    public static string GetFilePath() => UserSettingsPaths.GetSettingsFilePath();

    public static bool FileExists() => File.Exists(GetFilePath());

    /// <summary>
    /// Читает файл; при отсутствии <paramref name="text"/> = null и <paramref name="mtimeUtc"/> = <see cref="DateTime.MinValue"/>.
    /// При ошибке чтения — <paramref name="text"/> = null, mtime — с диска если файл ещё есть, иначе MinValue.
    /// </summary>
    public static void TryRead(out string? text, out DateTime mtimeUtc)
    {
        var path = GetFilePath();
        text = null;
        mtimeUtc = DateTime.MinValue;
        if (!File.Exists(path))
            return;
        try
        {
            text = File.ReadAllText(path);
            mtimeUtc = File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            mtimeUtc = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
    }

    public static bool TryGetLastWriteTimeUtc(out DateTime mtimeUtc)
    {
        var path = GetFilePath();
        mtimeUtc = default;
        if (!File.Exists(path))
            return false;
        try
        {
            mtimeUtc = File.GetLastWriteTimeUtc(path);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void WriteAllText(string toml, out DateTime mtimeUtcAfterWrite)
    {
        var path = GetFilePath();
        File.WriteAllText(path, toml);
        mtimeUtcAfterWrite = File.GetLastWriteTimeUtc(path);
    }
}
