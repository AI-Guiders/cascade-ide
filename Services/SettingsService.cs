using CascadeIDE.Models;

namespace CascadeIDE.Services;

public static class SettingsService
{
    private static readonly ISettingsValidationSpecification[] ValidationSpecifications =
    [
        new DisplaySettingsValidationSpecification()
    ];

    /// <summary>
    /// UTC mtime <c>settings.toml</c> на момент последнего успешного <see cref="Load"/> (или <see cref="DateTime.MinValue"/>, если файла не было).
    /// Если перед <see cref="Save"/> файл новее — подмешиваем <c>[presentation]</c> и <c>[display.screens]</c> с диска, чтобы ручные правки не затирались.
    /// </summary>
    private static DateTime _settingsFileMtimeUtcAtLastLoad = DateTime.MinValue;

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
        var tomlPath = GetSettingsPath();
        try
        {
            if (!File.Exists(tomlPath))
            {
                _settingsFileMtimeUtcAtLastLoad = DateTime.MinValue;
                return ValidateAndReturn(new CascadeIdeSettings());
            }

            var toml = File.ReadAllText(tomlPath);
            var settings = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(toml) ?? new CascadeIdeSettings();
            _settingsFileMtimeUtcAtLastLoad = File.GetLastWriteTimeUtc(tomlPath);
            return ValidateAndReturn(settings);
        }
        catch
        {
            _settingsFileMtimeUtcAtLastLoad = File.Exists(tomlPath)
                ? File.GetLastWriteTimeUtc(tomlPath)
                : DateTime.MinValue;
            return ValidateAndReturn(new CascadeIdeSettings());
        }
    }

    public static void Save(CascadeIdeSettings settings)
    {
        try
        {
            var path = GetSettingsPath();
            if (File.Exists(path))
            {
                var mtimeNow = File.GetLastWriteTimeUtc(path);
                if (mtimeNow > _settingsFileMtimeUtcAtLastLoad)
                {
                    try
                    {
                        var diskToml = File.ReadAllText(path);
                        var disk = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(diskToml);
                        if (disk is not null)
                            ApplyPresentationFromDisk(settings, disk);
                    }
                    catch
                    {
                        // merge не обязателен для сохранения остальных полей
                    }
                }
            }

            var toml = CascadeTomlSerializer.Serialize(settings);
            File.WriteAllText(path, toml);
            _settingsFileMtimeUtcAtLastLoad = File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }

    /// <summary>Перезаписать секции <c>presentation</c> и <c>display.screens</c> в <paramref name="target"/> из <paramref name="disk"/> (клон полей).</summary>
    internal static void ApplyPresentationFromDisk(CascadeIdeSettings target, CascadeIdeSettings disk)
    {
        var p = disk.Presentation;
        target.Presentation.Line = p.Line;
        target.Presentation.LineAlias = p.LineAlias;
        var g = p.Grammar;
        target.Presentation.Grammar = new PresentationGrammarSettings
        {
            Brackets = g.Brackets,
            BetweenScreens = g.BetweenScreens,
            BetweenZones = g.BetweenZones,
            Pfd = g.Pfd,
            Forward = g.Forward,
            Mfd = g.Mfd,
        };

        var s = disk.Display.Screens;
        target.Display.Screens.Topology = s.Topology;
        target.Display.Screens.Grammar = new PresentationGrammarSettings
        {
            Brackets = s.Grammar.Brackets,
            BetweenScreens = s.Grammar.BetweenScreens,
            BetweenZones = s.Grammar.BetweenZones,
            Pfd = s.Grammar.Pfd,
            Forward = s.Grammar.Forward,
            Mfd = s.Grammar.Mfd,
        };
    }

    private static CascadeIdeSettings ValidateAndReturn(CascadeIdeSettings settings)
    {
        foreach (var validationError in ValidationSpecifications.SelectMany(spec => spec.Validate(settings)))
            global::System.Diagnostics.Debug.WriteLine($"Settings validation: {validationError}");
        return settings;
    }
}
