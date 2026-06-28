using System.Collections.ObjectModel;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionExplorerTreeFilterTests
{
    [Fact]
    public void RebuildDisplayRoots_NoMatch_ClearsDisplay()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\ws\app.sln");
        var proj = SolutionItem.CreateProject("p", @"C:\ws\p\p.csproj");
        proj.Children.Add(SolutionItem.CreateFile("Alpha.cs", @"C:\ws\p\Alpha.cs"));
        root.Children.Add(proj);
        var source = new ObservableCollection<SolutionItem> { root };
        var display = new ObservableCollection<SolutionItem>();

        var index = new WorkspaceFileIndex();
        index.Invalidate(source, @"C:\ws\app.sln", @"C:\ws");
        SolutionExplorerTreeFilter.RebuildDisplayRoots(source, display, "Chain-lists", index);

        Assert.Empty(display);
    }

    [Fact]
    public void RebuildDisplayRoots_MatchingFile_ShowsPathToFile()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\ws\app.sln");
        var proj = SolutionItem.CreateProject("p", @"C:\ws\p\p.csproj");
        var file = SolutionItem.CreateFile("Chain-lists.cs", @"C:\ws\p\Chain-lists.cs");
        proj.Children.Add(file);
        root.Children.Add(proj);
        var source = new ObservableCollection<SolutionItem> { root };
        var display = new ObservableCollection<SolutionItem>();

        var index = new WorkspaceFileIndex();
        index.Invalidate(source, @"C:\ws\app.sln", @"C:\ws");
        SolutionExplorerTreeFilter.RebuildDisplayRoots(source, display, "Chain-lists", index);

        Assert.Single(display);
        Assert.Equal(root.FullPath, display[0].FullPath);
        Assert.Single(display[0].Children);
        Assert.Single(display[0].Children[0].Children);
        Assert.Equal(file.FullPath, display[0].Children[0].Children[0].FullPath);
        Assert.NotSame(file, display[0].Children[0].Children[0]);
    }

    [Fact]
    public void RebuildDisplayRoots_EmptyFilter_ReusesSourceReferences()
    {
        var root = SolutionItem.CreateFolder("dir");
        root.Children.Add(SolutionItem.CreateFile("a.cs", @"C:\ws\a.cs"));
        var source = new ObservableCollection<SolutionItem> { root };
        var display = new ObservableCollection<SolutionItem>();

        var index = new WorkspaceFileIndex();
        index.Invalidate(source, null, @"C:\ws");
        SolutionExplorerTreeFilter.RebuildDisplayRoots(source, display, "", index);

        Assert.Same(root, display[0]);
    }

    [Fact]
    public void RebuildDisplayRoots_SubstringOnProject_ShowsProjectAndMatches()
    {
        var root = SolutionItem.CreateSolution("CascadeIDE", @"C:\cascade\CascadeIDE.sln");
        var analyzers = SolutionItem.CreateProject("CascadeIDE.ArchitectureAnalyzers", @"C:\cascade\CascadeIDE.ArchitectureAnalyzers\CascadeIDE.ArchitectureAnalyzers.csproj");
        var other = SolutionItem.CreateProject("CascadeIDE.Tests", @"C:\cascade\CascadeIDE.Tests\CascadeIDE.Tests.csproj");
        root.Children.Add(analyzers);
        root.Children.Add(other);
        var source = new ObservableCollection<SolutionItem> { root };
        var display = new ObservableCollection<SolutionItem>();

        var index = new WorkspaceFileIndex();
        index.Invalidate(source, @"C:\cascade\CascadeIDE.sln", @"C:\cascade");
        SolutionExplorerTreeFilter.RebuildDisplayRoots(source, display, "Anal", index);

        Assert.Single(display);
        Assert.Single(display[0].Children);
        Assert.Equal(analyzers.FullPath, display[0].Children[0].FullPath);
    }
}
