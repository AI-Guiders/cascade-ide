using CascadeIDE.Services;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using TestContext = Xunit.TestContext;

namespace CascadeIDE.Tests.MonacoForward;

[Trait("Category", "MonacoForward")]
public sealed class CSharpLanguageServiceCompletionTests
{
    [Fact]
    public void MemberAccess_lists_type_members_not_global_scope()
    {
        const string path = @"D:\Fake\MemberComplete.cs";
        var src = """
            using System;
            class C
            {
                public string Title { get; set; }
                public int Count;
                public void Run() { }
                void M()
                {
                    var sb = new System.Text.StringBuilder();
                    sb.
                }
            }
            """;
        var (line, column) = AfterMarker(src, "sb.");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.NotEmpty(items);
        Assert.Contains(items, i => i.DisplayText == "Append" && i.Kind == CSharpLanguageService.CSharpCompletionKind.Method);
        Assert.Contains(items, i => i.DisplayText == "Length" && i.Kind == CSharpLanguageService.CSharpCompletionKind.Property);
        Assert.DoesNotContain(items, i => i.DisplayText == "Title");
        Assert.DoesNotContain(items, i => i.DisplayText == "while");
    }

    [Fact]
    public void MemberAccess_includes_property_and_field_on_own_type()
    {
        const string path = @"D:\Fake\OwnMembers.cs";
        var src = """
            class Box
            {
                public string Label;
                public int Size { get; set; }
                void M(Box b) { b. }
            }
            """;
        var (line, column) = AfterMarker(src, "b. ");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.Contains(items, i => i.DisplayText == "Label" && i.Kind == CSharpLanguageService.CSharpCompletionKind.Field);
        Assert.Contains(items, i => i.DisplayText == "Size" && i.Kind == CSharpLanguageService.CSharpCompletionKind.Property);
    }

    [Fact]
    public void ScopeCompletion_uses_names_not_entire_symbol_dump()
    {
        const string path = @"D:\Fake\Scope.cs";
        var src = """
            class C
            {
                void Outer()
                {
                    int local = 1;
                    loc
                }
            }
            """;
        var (line, column) = AfterMarker(src, "loc");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.Contains(items, i => i.DisplayText == "local");
        Assert.DoesNotContain(items, i => i.DisplayText == "Outer" && i.Kind == CSharpLanguageService.CSharpCompletionKind.Method);
    }

    [Fact]
    public void ScopeCompletion_acronym_matches_types_after_new_when_text_imported()
    {
        const string path = @"D:\Fake\Acronym.cs";
        var src = """
            using System.Text;
            class C { void M() { var x = new SB } }
            """;
        var (line, column) = AfterMarker(src, "SB");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.Contains(items, i => i.DisplayText == "StringBuilder");
        Assert.Contains(items, i => i.DisplayText == "SByte");
    }

    [Fact]
    public void ScopeCompletion_acronym_matches_types_in_project_with_global_usings()
    {
        var csproj = FindRepoFile(@"CasaField.Core\CasaField.Core.csproj");
        var path = Path.Combine(Path.GetDirectoryName(csproj)!, "AcronymSample.cs");
        var src = "class C { void M() { var x = new SB } }";
        var (line, column) = AfterMarker(src, "SB");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.Contains(items, i => i.DisplayText == "SByte");
    }

    [Fact]
    public void NamespaceDeclaration_lists_child_namespaces_not_statement_keywords()
    {
        const string path = @"D:\Fake\NsDecl.cs";
        var src = """
            namespace Casa.
            class C { }
            """;
        var (line, column) = AfterMarker(src, "namespace Casa.");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(items, i => i.DisplayText == "while");
        Assert.DoesNotContain(items, i => i.DisplayText == "class");
        Assert.DoesNotContain(items, i => i.DisplayText == "namespace");
    }

    [Fact]
    public void FileScopedNamespaceDeclaration_filters_prefix_without_keywords()
    {
        const string path = @"D:\Fake\FsNs.cs";
        var src = "namespace Cas";
        var (line, column) = AfterMarker(src, "Cas");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(items, i => i.DisplayText == "while");
        Assert.DoesNotContain(items, i => i.DisplayText == "catch");
    }

    [Fact]
    public void ScopeCompletion_acronym_matches_types()
    {
        const string path = @"D:\Fake\Acronym.cs";
        var src = """
            using System.Text;
            class C { void M() { SB } }
            """;
        var (line, column) = AfterMarker(src, "SB");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.Contains(items, i => i.DisplayText == "StringBuilder");
        Assert.Contains(items, i => i.DisplayText == "SByte");
    }

    [Fact]
    public void ScopeCompletion_includes_compiler_keywords_from_syntax_facts()
    {
        const string path = @"D:\Fake\Keywords.cs";
        var src = """
            class C
            {
                void M()
                {
                    rec
                }
            }
            """;
        var (line, column) = AfterMarker(src, "rec");
        var svc = new CSharpLanguageService();
        var items = svc.GetCompletionItems(path, src, line, column, TestContext.Current.CancellationToken);

        Assert.Contains(items, i => i.DisplayText == "record" && i.Kind == CSharpLanguageService.CSharpCompletionKind.Keyword);
    }

    private static (int Line, int Column) AfterMarker(string source, string marker)
    {
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Marker '{marker}' not found.");
        var position = index + marker.Length;
        var location = SourceText.From(source).Lines.GetLinePosition(position);
        return (location.Line + 1, location.Character + 1);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.GetFullPath(Path.Combine(dir, relativePath));
            if (File.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }

        throw new FileNotFoundException(relativePath);
    }
}
