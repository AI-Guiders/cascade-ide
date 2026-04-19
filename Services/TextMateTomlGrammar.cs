using TextMateSharp.Grammars;

namespace CascadeIDE.Services;

/// <summary>
/// TextMateSharp's embedded bundle has no TOML scope; we ship a VS Code-style grammar under <c>TextMateGrammars/toml/</c> (MIT, taplo — see folder README).
/// </summary>
public static class TextMateTomlGrammar
{
    /// <summary>Relative to <see cref="AppContext.BaseDirectory"/> unless <paramref name="baseDirectory"/> is set.</summary>
    public const string PackageJsonRelativePath = "TextMateGrammars/toml/package.json";

    private static readonly string[] s_embeddedTomlGrammarFiles =
    [
        "TextMateGrammars/toml/package.json",
        "TextMateGrammars/toml/syntaxes/toml.tmLanguage.json",
    ];

    private static string? s_cachedExtractedPackagePath;

    /// <summary>Loads the bundled TOML grammar into the registry (no-op if package file is missing on disk and in resources).</summary>
    public static void TryLoadInto(RegistryOptions registry, string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        var packagePath = Path.GetFullPath(Path.Combine(baseDirectory, PackageJsonRelativePath));
        if (File.Exists(packagePath))
        {
            registry.LoadFromLocalFile("TOML", packagePath, overwrite: false);
            return;
        }

        packagePath = EnsureExtractedPackagePathFromEmbedded();
        if (packagePath is null || !File.Exists(packagePath))
            return;
        registry.LoadFromLocalFile("TOML", packagePath, overwrite: false);
    }

    private static string? EnsureExtractedPackagePathFromEmbedded()
    {
        if (s_cachedExtractedPackagePath is not null && File.Exists(s_cachedExtractedPackagePath))
            return s_cachedExtractedPackagePath;

        var extractRoot = Path.Combine(SettingsService.GetSettingsDirectory(), "cache", "embedded-textmate-toml");
        try
        {
            Directory.CreateDirectory(extractRoot);
            foreach (var rel in s_embeddedTomlGrammarFiles)
            {
                if (!BundledAppContent.TryReadEmbeddedText(rel, out var text))
                    return null;
                var dest = Path.Combine(extractRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                var dir = Path.GetDirectoryName(dest);
                if (dir is not null)
                    Directory.CreateDirectory(dir);
                File.WriteAllText(dest, text);
            }

            var package = Path.Combine(extractRoot, PackageJsonRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(package))
                return null;
            s_cachedExtractedPackagePath = package;
            return package;
        }
        catch
        {
            return null;
        }
    }
}
