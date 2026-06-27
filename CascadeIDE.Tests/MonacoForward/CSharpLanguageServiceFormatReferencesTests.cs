using CascadeIDE.Services;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using TestContext = Xunit.TestContext;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CSharpLanguageServiceFormatReferencesTests
{
    [Fact]
    public void FormatDocument_normalizes_spacing()
    {
        const string path = @"D:\Fake\Fmt.cs";
        const string src = "class C{void M(){int x=1;}}";
        var svc = new CSharpLanguageService();
        var formatted = svc.FormatDocument(path, src, TestContext.Current.CancellationToken);

        Assert.Contains("class C", formatted);
        Assert.Contains("void M", formatted);
        Assert.NotEqual(src, formatted);
    }

    [Fact]
    public void OrganizeUsings_sorts_directives()
    {
        const string path = @"D:\Fake\Usings.cs";
        const string src = """
            using System.Text;
            using System;
            class C { }
            """;
        var svc = new CSharpLanguageService();
        var organized = svc.OrganizeUsings(path, src, TestContext.Current.CancellationToken);

        var systemIdx = organized.IndexOf("using System;", StringComparison.Ordinal);
        var textIdx = organized.IndexOf("using System.Text;", StringComparison.Ordinal);
        Assert.True(systemIdx >= 0);
        Assert.True(textIdx > systemIdx);
    }

    [Fact]
    public void FindReferencesInFile_finds_local_usages()
    {
        const string path = @"D:\Fake\Refs.cs";
        var src = """
            class C
            {
                void M()
                {
                    int count = 1;
                    count = count + 1;
                }
            }
            """;
        var (line, column) = AfterMarker(src, "count = count");
        var svc = new CSharpLanguageService();
        var refs = svc.FindReferencesInFile(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.True(refs.Count >= 2);
        Assert.Contains(refs, r => r.Line > 0 && r.Column > 0);
    }

    private static (int Line, int Column) AfterMarker(string source, string marker)
    {
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Marker '{marker}' not found.");
        var tokenStart = index;
        while (tokenStart > 0 && char.IsLetterOrDigit(source[tokenStart - 1]))
            tokenStart--;
        var location = SourceText.From(source).Lines.GetLinePosition(tokenStart);
        return (location.Line + 1, location.Character + 1);
    }
}
