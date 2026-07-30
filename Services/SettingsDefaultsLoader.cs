using System.Reflection;
using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Models;

namespace CascadeIDE.Services;

/// <summary>
/// Заводские настройки: <c>Settings/defaults-settings.toml</c> (диск под exe → embedded в GlassCore).
/// Пользовательский <c>%LocalAppData%\CascadeIDE\settings.toml</c> merge поверх (см. <see cref="DeserializeEffective"/>).
/// </summary>
public static class SettingsDefaultsLoader
{
    /// <summary>Относительный путь от <see cref="AppContext.BaseDirectory"/> (опциональный override поверх встроенного бандла).</summary>
    public const string BundledRelativePath = SettingsDefaultsPaths.BundledRelativePath;

    /// <summary>Assembly with embedded defaults (GlassCore). Hosts may override for tests.</summary>
    public static Assembly? EmbeddedDefaultsAssembly { get; set; }

    /// <summary>
    /// Текст шипнутого <c>defaults-settings.toml</c>: сначала файл под <see cref="AppContext.BaseDirectory"/>,
    /// иначе embedded resource в <see cref="EmbeddedDefaultsAssembly"/> / GlassCore.
    /// </summary>
    /// <exception cref="InvalidOperationException">Нет ни файла рядом с процессом, ни встроенного ресурса.</exception>
    public static string GetEmbeddedDefaultsToml()
    {
        var asm = EmbeddedDefaultsAssembly ?? typeof(SettingsDefaultsPaths).Assembly;
        var text = SettingsDefaultsPaths.TryReadToml(explicitPath: null, embeddedAssembly: asm, out _);
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException(
                $"Missing bundled {BundledRelativePath} (disk under AppContext.BaseDirectory or embedded resource in {asm.GetName().Name}).");
        }

        return text;
    }

    /// <summary>Эффективные настройки без пользовательского файла (embedded defaults).</summary>
    public static CascadeIdeSettings CreateDefault() => DeserializeEffective(null);

    /// <summary>Merge заводских дефолтов с пользовательским TOML (пусто/null — только дефолты).</summary>
    public static CascadeIdeSettings DeserializeEffective(string? userTomlNormalized)
    {
        var defaults = GetEmbeddedDefaultsToml();
        var merged = string.IsNullOrWhiteSpace(userTomlNormalized)
            ? defaults
            : TomlTableMerge.MergeTomlDocuments(defaults, userTomlNormalized);
        return CascadeTomlSerializer.Deserialize<CascadeIdeSettings>(merged)
            ?? throw new InvalidOperationException("Merged settings TOML did not deserialize to CascadeIdeSettings.");
    }
}
