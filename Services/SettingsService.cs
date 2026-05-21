using CascadeIDE.Features.Chat;
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
    /// Если перед <see cref="Save"/> файл новее — подмешиваем <c>[display.screens]</c> с диска, чтобы ручные правки не затирались.
    /// </summary>
    private static DateTime _settingsFileMtimeUtcAtLastLoad = DateTime.MinValue;

    public static string GetSettingsDirectory() => UserSettingsPaths.GetSettingsDirectory();

    public static string GetSettingsPath() => UserSettingsPaths.GetSettingsFilePath();

    public static CascadeIdeSettings Load()
    {
        UserSettingsTomlFileAccess.TryRead(out var toml, out var mtime);
        if (toml is null)
        {
            _settingsFileMtimeUtcAtLastLoad = mtime;
            IntercomSendTrace.InvalidateSettingsCache();
            return ValidateAndReturn(SettingsDefaultsLoader.DeserializeEffective(null));
        }

        try
        {
            var normalized = NormalizeFriendlySectionAliases(toml);
            var settings = SettingsDefaultsLoader.DeserializeEffective(normalized);
            _settingsFileMtimeUtcAtLastLoad = mtime;
            IntercomSendTrace.InvalidateSettingsCache();
            return ValidateAndReturn(settings);
        }
        catch
        {
            _settingsFileMtimeUtcAtLastLoad = mtime;
            IntercomSendTrace.InvalidateSettingsCache();
            return ValidateAndReturn(SettingsDefaultsLoader.DeserializeEffective(null));
        }
    }

    public static void Save(CascadeIdeSettings settings)
    {
        try
        {
            var path = UserSettingsTomlFileAccess.GetFilePath();
            if (UserSettingsTomlFileAccess.TryGetLastWriteTimeUtc(out var mtimeNow) && mtimeNow > _settingsFileMtimeUtcAtLastLoad)
            {
                try
                {
                    var diskToml = TextFileReadWrite.TryReadAllTextIfExists(path);
                    if (diskToml is not null)
                    {
                        var normalizedDisk = NormalizeFriendlySectionAliases(diskToml);
                        var disk = CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(normalizedDisk);
                        if (disk is not null)
                            ApplyPresentationFromDisk(settings, disk);
                    }
                }
                catch
                {
                    // merge не обязателен для сохранения остальных полей
                }
            }

            var toml = CascadeTomlSerializer.Serialize(settings);
            UserSettingsTomlFileAccess.WriteAllText(toml, out var writtenMtime);
            _settingsFileMtimeUtcAtLastLoad = writtenMtime;
            IntercomSendTrace.InvalidateSettingsCache();
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }

    /// <summary>Перезаписать <c>[display.screens]</c> в <paramref name="target"/> из <paramref name="disk"/> (клон полей).</summary>
    internal static void ApplyPresentationFromDisk(CascadeIdeSettings target, CascadeIdeSettings disk)
    {
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

    private static string NormalizeFriendlySectionAliases(string toml)
    {
        if (string.IsNullOrWhiteSpace(toml))
            return toml;

        return toml
            .Replace("[Editor.InlineHints]", "[editor.inline_hints]", StringComparison.Ordinal)
            .Replace("[editor.InlineHints]", "[editor.inline_hints]", StringComparison.Ordinal)
            .Replace("[Editor.inline_hints]", "[editor.inline_hints]", StringComparison.Ordinal);
    }
}
