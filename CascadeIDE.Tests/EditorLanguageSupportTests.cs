using CascadeIDE.Features.Editor.Application.Monaco;
using CascadeIDE.Features.Settings.DataAcquisition;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class EditorLanguageSupportTests
{
    [Theory]
    [InlineData(@"C:\repo\App.csproj", "xml")]
    [InlineData(@"C:\repo\App.fsproj", "xml")]
    [InlineData(@"C:\repo\App.props", "xml")]
    [InlineData(@"C:\repo\App.targets", "xml")]
    [InlineData(@"C:\repo\app.config", "xml")]
    [InlineData(@"C:\repo\View.axaml", "xml")]
    [InlineData(@"C:\repo\Program.cs", "csharp")]
    [InlineData(@"C:\repo\readme.md", "markdown")]
    public void GetMonacoLanguageId_MapsProjectAndMarkupFiles(string path, string expected) =>
        Assert.Equal(expected, EditorLanguageSupport.GetMonacoLanguageId(path));

    [Fact]
    public void CideEditorLanguageIds_uses_same_mapping_as_support_table() =>
        Assert.Equal(
            EditorLanguageSupport.GetMonacoLanguageId(@"D:\x\CascadeIDE.csproj"),
            CideEditorLanguageIds.FromFilePath(@"D:\x\CascadeIDE.csproj"));

    [Fact]
    public void BundledToml_Contains_csproj_and_csharp()
    {
        var toml = EditorLanguagesTomlLoader.GetEmbeddedBundledEditorLanguagesToml();
        Assert.Contains(".csproj", toml, StringComparison.Ordinal);
        Assert.Contains("id = \"csharp\"", toml, StringComparison.Ordinal);
    }

    [Fact]
    public void IsTextFilePath_Includes_plain_text_extensions()
    {
        Assert.True(EditorLanguageSupport.IsTextFilePath(@"C:\repo\notes.txt"));
        Assert.True(EditorLanguageSupport.IsTextFilePath(@"C:\repo\build.log"));
        Assert.True(EditorLanguageSupport.IsTextFilePath(@"C:\repo\Component.jsx"));
    }

    [Fact]
    public void MergeManifests_User_Replaces_Language_By_Id()
    {
        var bundled = new EditorLanguagesManifest
        {
            Languages =
            [
                new EditorLanguageEntry
                {
                    Id = "xml",
                    Display = "XML",
                    Extensions = [".csproj"],
                    Monaco = "xml",
                },
            ],
        };
        var user = new EditorLanguagesManifest
        {
            Languages =
            [
                new EditorLanguageEntry
                {
                    Id = "xml",
                    Display = "MSBuild XML",
                    Extensions = [".csproj", ".props"],
                    Monaco = "xml",
                },
            ],
        };

        var merged = EditorLanguagesTomlLoader.MergeManifests(bundled, user);
        var xml = Assert.Single(merged.Languages);
        Assert.Equal("MSBuild XML", xml.Display);
        Assert.Equal([".csproj", ".props"], xml.Extensions);
    }

    [Fact]
    public void UserOverlay_Can_Add_Custom_Extension()
    {
        try
        {
            EditorLanguagesTomlLoader.ReplaceManifestForTests = new EditorLanguagesManifest
            {
                Languages =
                [
                    new EditorLanguageEntry
                    {
                        Id = "dockerfile",
                        Display = "Dockerfile",
                        Extensions = [".dockerfile"],
                        Monaco = "plaintext",
                    },
                ],
            };
            EditorLanguageSupport.ClearCacheForTests();

            Assert.Equal("plaintext", EditorLanguageSupport.GetMonacoLanguageId(@"C:\repo\Dockerfile.dockerfile"));
            Assert.True(EditorLanguageSupport.IsTextFilePath(@"C:\repo\Dockerfile.dockerfile"));
        }
        finally
        {
            EditorLanguageSupport.ResetForTests();
        }
    }
}
