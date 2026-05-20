using TextMateSharp.Grammars;
using TextMateSharp.Themes;
using Xunit;

namespace CascadeIDE.Tests;

public class EditorLanguageSupportTests
{
    private static RegistryOptions CreateRegistryWithTomlBundle()
    {
        var registry = new RegistryOptions(ThemeName.DarkPlus);
        CascadeIDE.Services.TextMateTomlGrammar.TryLoadInto(registry, AppContext.BaseDirectory);
        return registry;
    }

    /// <summary>
    /// ADR 0015: every grammar extension in <see cref="CascadeIDE.Services.EditorLanguageSupport"/> must resolve after the same init as the app (incl. bundled TOML grammar).
    /// </summary>
    [Fact]
    public void ExtensionToGrammarExtension_AllResolveInTextMateRegistry()
    {
        var registry = CreateRegistryWithTomlBundle();

        foreach (var kv in CascadeIDE.Services.EditorLanguageSupport.ExtensionToGrammarExtension)
        {
            var lang = registry.GetLanguageByExtension(kv.Value);
            Assert.NotNull(lang);
        }
    }

    /// <summary>TOML uses the shipped taplo-derived TextMate grammar (<c>TextMateGrammars/toml</c>), not INI.</summary>
    [Fact]
    public void TomlFiles_MapToTomlExtensionAndResolve()
    {
        Assert.True(
            CascadeIDE.Services.EditorLanguageSupport.ExtensionToGrammarExtension.TryGetValue(".toml", out var grammarExt));
        Assert.Equal(".toml", grammarExt);
        var lang = CreateRegistryWithTomlBundle().GetLanguageByExtension(".toml");
        Assert.NotNull(lang);
        Assert.Equal("toml", lang.Id);
    }

    [Theory]
    [InlineData("src/Foo.cs", true)]
    [InlineData("notes.md", true)]
    [InlineData("app.toml", true)]
    [InlineData("readme.txt", true)]
    [InlineData("image.png", false)]
    [InlineData("noext", true)]
    public void IsTextFilePath_matches_editor_and_plain_text_extensions(string path, bool expected) =>
        Assert.Equal(expected, CascadeIDE.Services.EditorLanguageSupport.IsTextFilePath(path));
}
