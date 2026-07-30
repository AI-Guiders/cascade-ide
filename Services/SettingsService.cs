using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Features.Workspace.DataAcquisition;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

public static class SettingsService
{
    /// <summary>Host hook (e.g. IntercomSendTrace) — optional; GlassCore stays free of chat host.</summary>
    public static Action? AfterSettingsMutated { get; set; }

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

    /// <summary>
    /// Load typed SSOT: defaults → optional <c>.cascade/workspace.toml</c> under
    /// <paramref name="workspaceRoot"/> → user <c>settings.toml</c>.
    /// Null root skips repo overlay (tests / early boot).
    /// </summary>
    public static CascadeIdeSettings Load(string? workspaceRoot = null)
    {
        var workspaceToml = TryReadWorkspaceToml(workspaceRoot);
        UserSettingsTomlFileAccess.TryRead(out var toml, out var mtime);
        if (toml is null)
        {
            _settingsFileMtimeUtcAtLastLoad = mtime;
            AfterSettingsMutated?.Invoke();
            return ValidateAndReturn(SettingsDefaultsLoader.DeserializeEffective(null, workspaceToml));
        }

        try
        {
            var normalized = NormalizeFriendlySectionAliases(toml);
            var settings = SettingsDefaultsLoader.DeserializeEffective(normalized, workspaceToml);
            _settingsFileMtimeUtcAtLastLoad = mtime;
            AfterSettingsMutated?.Invoke();
            return ValidateAndReturn(settings);
        }
        catch
        {
            _settingsFileMtimeUtcAtLastLoad = mtime;
            AfterSettingsMutated?.Invoke();
            return ValidateAndReturn(SettingsDefaultsLoader.DeserializeEffective(null, workspaceToml));
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
            AfterSettingsMutated?.Invoke();
        }
        catch
        {
            // Игнорируем ошибки записи
        }
    }

    /// <summary>Перезаписать <c>[display.screens]</c> в <paramref name="target"/> из <paramref name="disk"/> (клон полей).</summary>
    public static void ApplyPresentationFromDisk(CascadeIdeSettings target, CascadeIdeSettings disk)
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

    static string? TryReadWorkspaceToml(string? workspaceRoot)
    {
        if (string.IsNullOrWhiteSpace(workspaceRoot))
            return null;

        try
        {
            var path = WorkspaceCascadePaths.GetWorkspaceTomlPath(workspaceRoot);
            return TextFileReadWrite.TryReadAllTextIfExists(path);
        }
        catch
        {
            return null;
        }
    }

    private static CascadeIdeSettings ValidateAndReturn(CascadeIdeSettings settings)
    {
        SettingsLegacyStringDefaults.Apply(settings);
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
