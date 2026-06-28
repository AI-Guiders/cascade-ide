using System.Collections.ObjectModel;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class SolutionTreeExpansionPolicyTests
{
    [Fact]
    public void ApplyDefaultExpansion_expands_solution_and_project_only()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\x.sln");
        var proj = SolutionItem.CreateProject("p", @"C:\x\p.csproj");
        var folder = SolutionItem.CreateFolder("src");
        folder.Children.Add(SolutionItem.CreateFile("f", @"C:\x\p\f.cs"));
        proj.Children.Add(folder);
        root.Children.Add(proj);

        SolutionTreeExpansionPolicy.ApplyDefaultExpansion([root]);

        Assert.True(root.IsExpanded);
        Assert.True(proj.IsExpanded);
        Assert.False(folder.IsExpanded);
    }

    [Fact]
    public void TryExpandPathTo_expands_ancestors()
    {
        var root = SolutionItem.CreateSolution("s", @"C:\x.sln");
        var proj = SolutionItem.CreateProject("p", @"C:\x\p.csproj");
        var file = SolutionItem.CreateFile("f", @"C:\x\p\f.cs");
        proj.Children.Add(file);
        root.Children.Add(proj);
        var roots = new ObservableCollection<SolutionItem> { root };

        proj.IsExpanded = false;
        root.IsExpanded = false;

        Assert.True(SolutionTreeExpansionPolicy.TryExpandPathTo(roots, file));
        Assert.True(root.IsExpanded);
        Assert.True(proj.IsExpanded);
    }
}
