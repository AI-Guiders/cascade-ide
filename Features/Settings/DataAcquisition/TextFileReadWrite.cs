namespace CascadeIDE.Features.Settings.DataAcquisition;

/// <summary>DAL: безопасное чтение/запись текста с диска для настроек и соседних JSON.</summary>
public static class TextFileReadWrite
{
    public static string? TryReadAllTextIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }
        catch
        {
            // fallthrough
        }
        return null;
    }

    public static void TryWriteAllText(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
        }
        catch
        {
            // caller may ignore
        }
    }
}
