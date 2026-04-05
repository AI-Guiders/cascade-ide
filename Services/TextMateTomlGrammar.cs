using TextMateSharp.Grammars;

namespace CascadeIDE.Services;

/// <summary>
/// TextMateSharp's embedded bundle has no TOML scope; we ship a VS Code-style grammar under <c>TextMateGrammars/toml/</c> (MIT, taplo — see folder README).
/// </summary>
public static class TextMateTomlGrammar
{
    /// <summary>Relative to <see cref="AppContext.BaseDirectory"/> unless <paramref name="baseDirectory"/> is set.</summary>
    public const string PackageJsonRelativePath = "TextMateGrammars/toml/package.json";

    /// <summary>Loads the bundled TOML grammar into the registry (no-op if package file is missing).</summary>
    public static void TryLoadInto(RegistryOptions registry, string? baseDirectory = null)
    {
        baseDirectory ??= AppContext.BaseDirectory;
        var packagePath = Path.GetFullPath(Path.Combine(baseDirectory, PackageJsonRelativePath));
        if (!File.Exists(packagePath))
            return;
        registry.LoadFromLocalFile("TOML", packagePath, overwrite: false);
    }
}
