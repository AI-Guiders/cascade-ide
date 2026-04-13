#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CascadeIDE.Services;

/// <summary>
/// Встроенные копии шипнутых файлов (см. <c>EmbeddedResource</c> в csproj): при отсутствии файла на диске рядом с exe читается ресурс сборки.
/// Имена ресурсов: <c>CascadeIDE.</c> + относительный путь с <c>/</c> → <c>.</c> (напр. <c>Themes/dark-theme.json</c> → <c>CascadeIDE.Themes.dark-theme.json</c>).
/// </summary>
public static class BundledAppContent
{
    private static readonly Assembly s_assembly = typeof(BundledAppContent).Assembly;
    private const string ResourcePrefix = "CascadeIDE.";

    /// <param name="relativePath">Относительно корня приложения, слеши <c>/</c>, напр. <c>UiModes/index.toml</c>.</param>
    public static bool TryReadEmbeddedText(string relativePath, [NotNullWhen(true)] out string? text)
    {
        text = null;
        var normalized = NormalizeRelative(relativePath);
        if (normalized.Length == 0)
            return false;
        var name = ResourcePrefix + normalized.Replace('/', '.');
        using var stream = s_assembly.GetManifestResourceStream(name);
        if (stream is null)
            return false;
        using var reader = new StreamReader(stream);
        text = reader.ReadToEnd();
        return !string.IsNullOrWhiteSpace(text);
    }

    /// <summary>Сначала файл под <see cref="AppContext.BaseDirectory"/>, затем встроенный ресурс.</summary>
    public static bool TryReadDiskThenEmbedded(string relativePath, [NotNullWhen(true)] out string? text)
    {
        text = null;
        var normalized = NormalizeRelative(relativePath);
        if (normalized.Length == 0)
            return false;
        var disk = Path.Combine(AppContext.BaseDirectory, normalized.Replace('/', Path.DirectorySeparatorChar));
        try
        {
            if (File.Exists(disk))
            {
                text = File.ReadAllText(disk);
                return true;
            }
        }
        catch
        {
            // fallback на ресурс
        }

        return TryReadEmbeddedText(normalized, out text);
    }

    private static string NormalizeRelative(string relativePath) =>
        relativePath.Replace('\\', '/').TrimStart('/').Trim();
}
