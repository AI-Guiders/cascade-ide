using System.Linq;
using CascadeIDE.Services;
using Xunit;
using TestContext = Xunit.TestContext;

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
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        var h = Assert.Single(parts);
        Assert.Contains("int", h.Label, StringComparison.Ordinal);
        var i = src.IndexOf("var", StringComparison.Ordinal);
        Assert.True(i >= 0);
        Assert.Equal(i + 3, h.AnchorOffset);
        Assert.Equal(' ', src[h.AnchorOffset]);
    }

    [Fact]
    public void GetVarInlayHints_ForEachVar_ReturnsElementType()
    {
        const string path = @"D:\Fake\InlayForeach.cs";
        const string src = "class C { void M() { foreach (var x in new int[0]) { } } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        var h = Assert.Single(parts);
        Assert.Contains("int", h.Label, StringComparison.Ordinal);
        var j = src.IndexOf("var", StringComparison.Ordinal);
        Assert.Equal(j + 3, h.AnchorOffset);
    }

    [Fact]
    public void GetVarInlayHints_NonCs_ReturnsEmpty()
    {
        var svc = new CSharpLanguageService();
        Assert.Empty(svc.GetVarInlayHintsForFile(@"D:\a.txt", "x", TestContext.Current.CancellationToken));
    }

    [Fact]
    public void GetVarInlayHints_Invocation_SkipsInlayWhenIdentifierEqualsParamName()
    {
        const string path = @"D:\Fake\InlayParamSameName.cs";
        const string src = """
            static void F(string[] args) { }
            static void M(string[] args) { F(args); }
            """;
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.DoesNotContain(parts, p => p.Label.Contains("args", StringComparison.Ordinal) && p.Label.Contains(':'));
    }

    [Fact]
    public void GetVarInlayHints_ParamsArray_EmitsOneLabelForParamsParameter()
    {
        const string path = @"D:\Fake\InlayParams.cs";
        const string src = "class C { static void M(params int[] items) { } void K() { M(1, 2, 3); } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Single(parts, p => p.Label.Contains("items", StringComparison.Ordinal) && p.Label.Contains(':'));
        int call = src.IndexOf("M(1, 2, 3)", StringComparison.Ordinal);
        int one = call + 2;
        Assert.Equal(one, parts.First(p => p.Label.Contains("items", StringComparison.Ordinal)).AnchorOffset);
    }

    [Fact]
    public void GetVarInlayHints_Invocation_InsertsParameterNameLabels()
    {
        const string path = @"D:\Fake\InlayParamInvoke.cs";
        const string src = """
            class C
            {
                void T(int alpha, int beta) { }
                void M() { T(1, 2); }
            }
            """;
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Contains(parts, p => p.Label.Contains("alpha", StringComparison.Ordinal) && p.Label.Contains(':'));
        Assert.Contains(parts, p => p.Label.Contains("beta", StringComparison.Ordinal) && p.Label.Contains(':'));
        int call = src.IndexOf("T(1, 2)", StringComparison.Ordinal);
        int one = call + 2;
        int two = call + 5; // "T(1, 2)" → '2' после ", "
        Assert.Equal(one, parts.First(p => p.Label.Contains("alpha", StringComparison.Ordinal)).AnchorOffset);
        Assert.Equal(two, parts.First(p => p.Label.Contains("beta", StringComparison.Ordinal)).AnchorOffset);
    }

    [Fact]
    public void GetVarInlayHints_NamedArguments_SkipsInlays()
    {
        const string path = @"D:\Fake\InlayNamed.cs";
        const string src = "class C { void T(int a, int b) { } void M() { T(a: 1, b: 2); } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Empty(parts);
    }

    [Fact]
    public void GetVarInlayHints_ObjectCreation_InsertsParameterNameLabel()
    {
        const string path = @"D:\Fake\InlayNew.cs";
        const string src = "class C { C(int kappa) { } void M() { new C(9); } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        var h = Assert.Single(parts, p => p.Label.Contains("kappa", StringComparison.Ordinal));
        int idx = src.IndexOf("9", StringComparison.Ordinal);
        Assert.Equal(idx, h.AnchorOffset);
    }

    [Fact]
    public void GetVarInlayHints_BclObjectCreationStringArg_InsertsMessageLabel()
    {
        const string path = @"D:\Fake\InlayBcl.cs";
        const string src = "class C { void M() { new System.IO.FileNotFoundException(\"x\"); } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Contains(parts, p => p.Label.Contains("message", StringComparison.Ordinal) && p.Label.Contains(':'));
    }

    [Fact]
    public void GetVarInlayHints_InvalidOperationException_InsertsMessageLabel()
    {
        const string path = @"D:\Fake\InlayInvalidOperation.cs";
        const string src = "class C { void M() { throw new InvalidOperationException(\"x\"); } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Contains(parts, p => p.Label.Contains("message", StringComparison.Ordinal) && p.Label.Contains(':'));
    }

    [Fact]
    public void GetVarInlayHints_TargetTypedNew_InsertsMessageLabel()
    {
        const string path = @"D:\Fake\InlayTargetTypedNew.cs";
        const string src = "class C { void M() { System.Exception ex = new(\"x\"); } }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Contains(parts, p => p.Label.Contains("message", StringComparison.Ordinal) && p.Label.Contains(':'));
    }

    [Fact]
    public void GetVarInlayHints_Indexer_InsertsParameterNameLabels()
    {
        const string path = @"D:\Fake\InlayIndex.cs";
        const string src = "class C { int this[int iota, int kappa] => 0; int M() => this[3, 4]; }";
        var svc = new CSharpLanguageService();
        var parts = svc.GetVarInlayHintsForFile(path, src, TestContext.Current.CancellationToken);
        Assert.Contains(parts, p => p.Label.Contains("iota", StringComparison.Ordinal));
        Assert.Contains(parts, p => p.Label.Contains("kappa", StringComparison.Ordinal));
    }
}
