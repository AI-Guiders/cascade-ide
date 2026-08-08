using CascadeIDE.GlassCore.Presentation;
using Xunit;

namespace CascadeIDE.Tests;

public class GlassFacePagePolicyTests
{
    [Theory]
    [InlineData("note.ru.md", "MarkdownPreview")]
    [InlineData(@"D:\kb\a.markdown", "MarkdownPreview")]
    [InlineData("README.mdown", "MarkdownPreview")]
    [InlineData("Foo.cs", "Editor")]
    [InlineData("x.json", "Editor")]
    [InlineData(null, "Editor")]
    [InlineData("", "Editor")]
    public void Resolve_path_kind_table(string? path, string page) =>
        Assert.Equal(page, GlassFacePagePolicy.Resolve(path));

    [Fact]
    public void Resolve_explicit_override_wins()
    {
        Assert.Equal(
            "Editor",
            GlassFacePagePolicy.Resolve("note.md", mfdOverride: "Editor"));
        Assert.Equal(
            "MarkdownPreview",
            GlassFacePagePolicy.Resolve("Foo.cs", mfdOverride: "MarkdownPreview"));
    }

    [Fact]
    public void IsDocumentFacePage_editor_and_preview()
    {
        Assert.True(GlassFacePagePolicy.IsDocumentFacePage("Editor"));
        Assert.True(GlassFacePagePolicy.IsDocumentFacePage("MarkdownPreview"));
        Assert.False(GlassFacePagePolicy.IsDocumentFacePage("WebAiPortal"));
    }
}
