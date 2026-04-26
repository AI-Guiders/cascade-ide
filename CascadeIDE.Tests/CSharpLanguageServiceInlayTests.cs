using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public class CSharpLanguageServiceInlayTests
{
    [Fact]
    public void GetVarInlayHints_ForVarLocal_ReturnsInferredTypeLabel()
    {
        const string path = @"D:\Fake\InlayVar.cs";
        var src = """
            class C
            {
                void M()
                {
                    var x = 1;
                }
            }
            """;
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src);
        var h = Assert.Single(parts);
        Assert.Contains("int", h.Label, StringComparison.Ordinal);
        Assert.True(h.Line1 > 0);
    }

    [Fact]
    public void GetVarInlayHints_ForEachVar_ReturnsElementType()
    {
        const string path = @"D:\Fake\InlayForeach.cs";
        const string src = "class C { void M() { foreach (var x in new int[0]) { } } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src);
        var h = Assert.Single(parts);
        Assert.Contains("int", h.Label, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVarInlayHints_NonCs_ReturnsEmpty()
    {
        var svc = new CSharpLanguageService();
        Assert.Empty(svc.GetVarInlayHintsForFile(@"D:\a.txt", "x"));
    }
}
