using System.Text.Json;
using CascadeIDE.Features.Settings.DataAcquisition;

namespace CascadeIDE.Services;

/// <summary>Список расширений редактора с подсветкой синтаксиса (источник: <c>Settings/editor-languages.toml</c>).</summary>
public static class EditorLanguageSupport
{
    private readonly record struct LanguageDescriptor(
        string Extension,
        string DisplayName,
        string MonacoLanguageId,
        bool Attach);

    private static Lazy<LanguageIndex> s_index = new(BuildIndex);

    private static LanguageIndex Current => s_index.Value;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    /// <summary>Пары расширение → краткое имя языка (для отображения и MCP).</summary>
    public static IReadOnlyList<(string Extension, string LanguageName)> Supported =>
        Current.Supported;

    /// <summary>Только тесты: сброс кэша индекса (manifest overlay не трогает).</summary>
    internal static void ClearCacheForTests() => s_index = new Lazy<LanguageIndex>(BuildIndex);

    /// <summary>Только тесты: сброс overlay manifest и кэша индекса.</summary>
    internal static void ResetForTests()
    {
        EditorLanguagesTomlLoader.ClearCacheForTests();
        ClearCacheForTests();
    }

    /// <summary>Файл читается как текст (excerpt @ send, attach): расширение из manifest или plain-text; без расширения — да.</summary>
    public static bool IsTextFilePath(string path)
    {
        var ext = Path.GetExtension(path);
        if (ext.Length == 0)
            return true;

        if (Current.PlainTextExtensions.Contains(ext))
            return true;

        return Current.ByExtension.TryGetValue(ext, out var lang) && lang.Attach;
    }

    /// <summary>Краткий текст для настроек: «C#, Markdown, XML/XAML, JSON, SQL, HTML, CSS, …».</summary>
    public static string GetSummary()
    {
        var names = Supported
            .Select(t => t.LanguageName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return string.Join(", ", names);
    }

    /// <summary>JSON для MCP: массив объектов { "extension": ".cs", "language": "C#" }.</summary>
    public static string GetJson()
    {
        var list = Supported
            .GroupBy(static t => t.Extension, StringComparer.OrdinalIgnoreCase)
            .Select(static g => new { extension = g.Key, language = g.First().LanguageName })
            .OrderBy(static x => x.extension)
            .ToList();
        return JsonSerializer.Serialize(list, JsonOptions);
    }

    /// <summary>Monaco <c>languageId</c> для Forward host по пути файла (built-in + Monarch pack).</summary>
    public static string GetMonacoLanguageId(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return "plaintext";

        var ext = Path.GetExtension(filePath);
        return ext.Length != 0 && Current.ByExtension.TryGetValue(ext, out var lang)
            ? lang.MonacoLanguageId
            : "plaintext";
    }

    private static LanguageIndex BuildIndex()
    {
        var manifest = EditorLanguagesTomlLoader.LoadMergedManifest();
        var byExtension = new Dictionary<string, LanguageDescriptor>(StringComparer.OrdinalIgnoreCase);
        var supported = new List<(string Extension, string LanguageName)>();
        var plainText = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in manifest.Languages)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                continue;

            var display = string.IsNullOrWhiteSpace(entry.Display) ? entry.Id : entry.Display;
            var monaco = string.IsNullOrWhiteSpace(entry.Monaco) ? entry.Id : entry.Monaco;
            foreach (var extension in entry.Extensions)
            {
                if (extension.Length == 0)
                    continue;

                var descriptor = new LanguageDescriptor(extension, display, monaco, entry.Attach);
                byExtension[extension] = descriptor;
                supported.Add((extension, display));
            }
        }

        foreach (var entry in manifest.PlainText)
        {
            if (!entry.Attach)
                continue;

            foreach (var extension in entry.Extensions)
            {
                if (extension.Length == 0)
                    continue;
                plainText.Add(extension);
            }
        }

        supported.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Extension, b.Extension));
        return new LanguageIndex(byExtension, supported, plainText);
    }

    private sealed record LanguageIndex(
        IReadOnlyDictionary<string, LanguageDescriptor> ByExtension,
        IReadOnlyList<(string Extension, string LanguageName)> Supported,
        IReadOnlySet<string> PlainTextExtensions);
}
