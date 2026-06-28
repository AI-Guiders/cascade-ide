using CascadeIDE.Contracts;
using CascadeIDE.Models;
using CascadeIDE.Services;

namespace CascadeIDE.Features.Settings.DataAcquisition;

/// <summary>
/// DAL: merged-манифест из шипнутого <c>Settings/editor-languages.toml</c> и
/// <c>%LocalAppData%\CascadeIDE\editor-languages.toml</c> (оверлей по <see cref="EditorLanguageEntry.Id"/>).
/// </summary>
[IoBoundary]
public static class EditorLanguagesTomlLoader
{
    public const string BundledRelativePath = "Settings/editor-languages.toml";

    /// <summary>Только тесты: подменить merged manifest без диска.</summary>
    internal static EditorLanguagesManifest? ReplaceManifestForTests { get; set; }

    /// <summary>Сброс кэша merged manifest (только тесты).</summary>
    internal static void ClearCacheForTests() => ReplaceManifestForTests = null;

    public static EditorLanguagesManifest LoadMergedManifest()
    {
        if (ReplaceManifestForTests is not null)
            return CloneManifest(ReplaceManifestForTests);

        var bundled = LoadManifestFromPath(
            UserSettingsPaths.GetBundledEditorLanguagesFilePath(),
            BundledRelativePath);
        var user = LoadManifestFromPath(UserSettingsPaths.GetEditorLanguagesUserFilePath(), embeddedRelativeFallback: null);
        return MergeManifests(bundled, user);
    }

    public static string GetEmbeddedBundledEditorLanguagesToml()
    {
        if (!BundledAppContent.TryReadDiskThenEmbedded(BundledRelativePath, out var text) || string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(
                $"Missing bundled {BundledRelativePath} (disk under AppContext.BaseDirectory or embedded resource in CascadeIDE assembly).");
        return text;
    }

    internal static EditorLanguagesManifest MergeManifests(EditorLanguagesManifest bundled, EditorLanguagesManifest user)
    {
        var languages = new Dictionary<string, EditorLanguageEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in bundled.Languages)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                continue;
            languages[entry.Id.Trim()] = CloneLanguage(entry);
        }

        foreach (var entry in user.Languages)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                continue;
            languages[entry.Id.Trim()] = CloneLanguage(entry);
        }

        var plainText = new List<EditorPlainTextEntry>();
        foreach (var entry in bundled.PlainText)
            plainText.Add(ClonePlainText(entry));
        foreach (var entry in user.PlainText)
            plainText.Add(ClonePlainText(entry));

        return new EditorLanguagesManifest
        {
            SchemaVersion = user.SchemaVersion > 0 ? user.SchemaVersion : bundled.SchemaVersion,
            Languages = languages.Values.OrderBy(static x => x.Id, StringComparer.OrdinalIgnoreCase).ToList(),
            PlainText = plainText,
        };
    }

    private static EditorLanguagesManifest LoadManifestFromPath(string path, string? embeddedRelativeFallback)
    {
        string? text = null;
        if (File.Exists(path))
            text = File.ReadAllText(path);
        else if (embeddedRelativeFallback is not null && BundledAppContent.TryReadEmbeddedText(embeddedRelativeFallback, out var emb))
            text = emb;

        if (string.IsNullOrWhiteSpace(text))
            return new EditorLanguagesManifest();

        try
        {
            return CascadeTomlSerializer.Deserialize<EditorLanguagesManifest>(text.Trim()) ?? new EditorLanguagesManifest();
        }
        catch
        {
            return new EditorLanguagesManifest();
        }
    }

    private static EditorLanguageEntry CloneLanguage(EditorLanguageEntry entry) =>
        new()
        {
            Id = entry.Id.Trim(),
            Display = entry.Display.Trim(),
            Extensions = entry.Extensions
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => NormalizeExtension(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Monaco = string.IsNullOrWhiteSpace(entry.Monaco) ? null : entry.Monaco.Trim(),
            Attach = entry.Attach,
        };

    private static EditorPlainTextEntry ClonePlainText(EditorPlainTextEntry entry) =>
        new()
        {
            Extensions = entry.Extensions
                .Where(static x => !string.IsNullOrWhiteSpace(x))
                .Select(static x => NormalizeExtension(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Attach = entry.Attach,
        };

    private static EditorLanguagesManifest CloneManifest(EditorLanguagesManifest manifest) =>
        new()
        {
            SchemaVersion = manifest.SchemaVersion,
            Languages = manifest.Languages.Select(CloneLanguage).ToList(),
            PlainText = manifest.PlainText.Select(ClonePlainText).ToList(),
        };

    internal static string NormalizeExtension(string raw)
    {
        var trimmed = raw.Trim();
        if (trimmed.Length == 0)
            return trimmed;
        return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
    }
}
