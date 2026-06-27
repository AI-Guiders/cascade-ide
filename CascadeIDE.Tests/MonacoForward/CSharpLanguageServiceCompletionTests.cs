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

    private static (int Line, int Column) AfterMarker(string source, string marker)
    {
        var index = source.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(index >= 0, $"Marker '{marker}' not found.");
        var position = index + marker.Length;
        var location = SourceText.From(source).Lines.GetLinePosition(position);
        return (location.Line + 1, location.Character + 1);
    }
}
