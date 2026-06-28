using System.Collections.ObjectModel;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceFileIndexTests
{
    [Fact]
    public void Search_prefix_rank_orders_starts_with_before_contains()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\ws\app.sln");
        var proj = SolutionItem.CreateProject("p", @"C:\ws\p\p.csproj");
        var fileA = SolutionItem.CreateFile("FooBar.cs", @"C:\ws\p\FooBar.cs");
        var fileB = SolutionItem.CreateFile("Other.cs", @"C:\ws\p\sub\XFooBar.cs");
        proj.Children.Add(fileA);
        proj.Children.Add(fileB);
        root.Children.Add(proj);

        var roots = new ObservableCollection<SolutionItem> { root };
        var index = new WorkspaceFileIndex();
        index.Invalidate(roots, @"C:\ws\app.sln", @"C:\ws");

        var matches = index.Search("foo", 10);
        Assert.True(matches.Count >= 2);
        Assert.Equal(@"p/FooBar.cs", matches[0].InsertPath, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Slash_provider_uses_same_index_ranking()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\ws\app.sln");
        var proj = SolutionItem.CreateProject("p", @"C:\ws\p\p.csproj");
        proj.Children.Add(SolutionItem.CreateFile("Alpha.cs", @"C:\ws\p\Alpha.cs"));
        root.Children.Add(proj);
        var roots = new ObservableCollection<SolutionItem> { root };

        var slash = new Features.Chat.WorkspaceFileSlashCompletionProvider(
            () => @"C:\ws\app.sln",
            () => roots,
            () => @"C:\ws");
        var matches = slash.GetMatches("alp", 5);
        Assert.Single(matches);
        Assert.Equal("p/Alpha.cs", matches[0].InsertPath, StringComparer.OrdinalIgnoreCase);
    }
}
