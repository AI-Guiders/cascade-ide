using CascadeIDE.Models;
using OutWit.Common.Json;

namespace CascadeIDE.Services;

/// <summary>Чтение/запись API-ключей в %LocalAppData%\CascadeIDE\ai-keys.json (не коммитить).</summary>
public static class AiKeysStorage
{
    private static string GetPath() => Path.Combine(SettingsService.GetSettingsDirectory(), "ai-keys.json");

    public static AiKeys Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path))
                return new AiKeys();
            return File.ReadAllText(path).FromJsonString<AiKeys>() ?? new AiKeys();
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
            File.WriteAllText(GetPath(), keys.ToJsonString(indented: true));
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }
}
