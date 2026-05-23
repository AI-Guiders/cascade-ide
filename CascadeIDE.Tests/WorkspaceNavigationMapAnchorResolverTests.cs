using CascadeIDE.Features.WorkspaceNavigation.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationMapAnchorResolverTests
{
    [Fact]
    public void Resolve_prefers_current_path_when_in_solution()
    {
        var solution = new[] { @"C:\repo\A.cs", @"C:\repo\B.cs" };
        var resolved = WorkspaceNavigationMapAnchorResolver.Resolve(
            @"C:\repo\B.cs",
            [],
            solution);
        Assert.Equal(@"C:\repo\B.cs", resolved);
    }

    [Fact]
    public void Resolve_falls_back_to_open_document()
    {
        var solution = new[] { @"C:\repo\A.cs", @"C:\repo\Open.cs" };
        var resolved = WorkspaceNavigationMapAnchorResolver.Resolve(
            null,
            [@"C:\repo\Open.cs"],
            solution);
        Assert.Equal(@"C:\repo\Open.cs", resolved);
    }

    [Fact]
    public void Resolve_falls_back_to_program_cs()
    {
        var solution = new[] { @"C:\repo\Utils.cs", @"C:\repo\Program.cs", @"C:\repo\Z.cs" };
        var resolved = WorkspaceNavigationMapAnchorResolver.Resolve(null, [], solution);
        Assert.Equal(@"C:\repo\Program.cs", resolved);
    }

    [Fact]
    public void Resolve_returns_null_when_no_cs_in_solution()
    {
        var resolved = WorkspaceNavigationMapAnchorResolver.Resolve(null, [], [@"C:\repo\readme.md"]);
        Assert.Null(resolved);
    }
}
