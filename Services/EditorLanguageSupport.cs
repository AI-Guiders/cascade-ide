using System.Text.Json;

namespace CascadeIDE.Services;

/// <summary>Список расширений редактора с подсветкой синтаксиса (один источник правды для маппинга, MCP и настроек).</summary>
public static class EditorLanguageSupport
{
    /// <summary>Пары расширение → краткое имя языка (для отображения и MCP). Расширения должны резолвиться в <see cref="TextMateSharp.Grammars.RegistryOptions"/> после той же инициализации, что в приложении (в т.ч. <see cref="TextMateTomlGrammar.TryLoadInto"/> для TOML).</summary>
    public static IReadOnlyList<(string Extension, string LanguageName)> Supported { get; } =
    [
        (".bat", "Batch"),
        (".cake", "Cake"),
        (".cjs", "JavaScript"),
        (".cs", "C#"),
        (".csx", "C#"),
        (".cshtml", "Razor"),
        (".css", "CSS"),
        (".go", "Go"),
        (".htm", "HTML"),
        (".html", "HTML"),
        (".js", "JavaScript"),
        (".json", "JSON"),
        (".less", "Less"),
        (".markdown", "Markdown"),
        (".md", "Markdown"),
        (".mjs", "JavaScript"),
        (".mts", "TypeScript"),
        (".cts", "TypeScript"),
        (".ps1", "PowerShell"),
        (".psd1", "PowerShell"),
        (".psm1", "PowerShell"),
        (".py", "Python"),
        (".razor", "Razor"),
        (".rs", "Rust"),
        (".scss", "SCSS"),
        (".sh", "Bash"),
        (".sql", "SQL"),
        (".toml", "TOML"),
        (".ts", "TypeScript"),
        (".tsx", "TypeScript React"),
        (".xml", "XML"),
        (".axaml", "AXAML"),
        (".xaml", "XAML"),
        (".csproj", "XML"),
        (".config", "XML"),
        (".props", "XML"),
        (".targets", "XML"),
        (".yaml", "YAML"),
        (".yml", "YAML"),
    ];

    /// <summary>Расширения файлов → расширение грамматики для GetLanguageByExtension (встроенный бандл + при необходимости шипнутые грамматики, см. TextMateGrammars).</summary>
    public static IReadOnlyDictionary<string, string> ExtensionToGrammarExtension { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".bat"] = ".bat",
        [".cake"] = ".cake",
        [".cjs"] = ".cjs",
        [".cs"] = ".cs",
        [".csx"] = ".csx",
        [".cshtml"] = ".cshtml",
        [".css"] = ".css",
        [".go"] = ".go",
        [".htm"] = ".htm",
        [".html"] = ".html",
        [".js"] = ".js",
        [".mjs"] = ".mjs",
        [".json"] = ".json",
        [".less"] = ".less",
        [".md"] = ".md",
        [".markdown"] = ".markdown",
        [".mts"] = ".ts",
        [".cts"] = ".ts",
        [".ps1"] = ".ps1",
        [".psm1"] = ".psm1",
        [".psd1"] = ".psd1",
        [".py"] = ".py",
        [".razor"] = ".razor",
        [".rs"] = ".rs",
        [".scss"] = ".scss",
        [".sh"] = ".sh",
        [".sql"] = ".sql",
        [".toml"] = ".toml",
        [".ts"] = ".ts",
        [".tsx"] = ".tsx",
        [".xml"] = ".xml",
        [".axaml"] = ".xml",
        [".xaml"] = ".xml",
        [".csproj"] = ".xml",
        [".config"] = ".xml",
        [".props"] = ".xml",
        [".targets"] = ".xml",
        [".yaml"] = ".yaml",
        [".yml"] = ".yml",
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private static readonly HashSet<string> SupportedExtensionSet = new(
        Supported.Select(static t => t.Extension),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Плоский текст без отдельной грамматики в TextMate (excerpt, attach file).</summary>
    private static readonly HashSet<string> PlainTextExtensionsWithoutGrammar = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".log",
        ".jsx",
    };

    /// <summary>Файл читается как текст (excerpt @ send, attach): расширение из <see cref="Supported"/> или plain-text supplement; без расширения — да.</summary>
    public static bool IsTextFilePath(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Length == 0
            || SupportedExtensionSet.Contains(ext)
            || PlainTextExtensionsWithoutGrammar.Contains(ext);
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
}
