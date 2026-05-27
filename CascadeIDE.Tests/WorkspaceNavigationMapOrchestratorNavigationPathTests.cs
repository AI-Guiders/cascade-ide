using System.IO;
using CascadeIDE.Features.WorkspaceNavigation.Application;
using CascadeIDE.Models;
using CascadeIDE.Services;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class WorkspaceNavigationMapOrchestratorNavigationPathTests
{
    [Fact]
    public void ResolveNavigationPathForGraphJson_ControlFlow_prefers_current_cs_when_in_solution_files()
    {
        var foo = Path.Combine(Path.GetTempPath(), "cf-nav-prefers-current", "Foo.cs");
        var other = Path.Combine(Path.GetTempPath(), "cf-nav-prefers-current", "Other.cs");
        var list = new[] { foo, other };

        var p = WorkspaceNavigationMapOrchestrator.ResolveNavigationPathForGraphJson(
            CodeNavigationMapLevelKind.ControlFlow,
            foo,
            other,
            list);

        Assert.True(EditorTextCoordinateUtilities.PathsReferToSameFile(p!, foo));
    }

    [Fact]
    public void ResolveNavigationPathForGraphJson_ControlFlow_uses_active_cs_when_not_tracked_like_solution_members()
    {
        var current = @"C:\workspace\Experimental\Sandbox.cs";

        var p = WorkspaceNavigationMapOrchestrator.ResolveNavigationPathForGraphJson(
            CodeNavigationMapLevelKind.ControlFlow,
            current,
            @"C:\workspace\Legacy\Program.cs",
            []);

        Assert.Equal(current, p);
    }

    [Fact]
    public void ResolveNavigationPathForGraphJson_File_level_keeps_fallback_order()
    {
        var list = new[] { @"D:\a\X.cs" };
        var p = WorkspaceNavigationMapOrchestrator.ResolveNavigationPathForGraphJson(
            CodeNavigationMapLevelKind.File,
            @"D:\a\Y.cs",
            @"D:\a\X.cs",
            list);
        Assert.Equal(@"D:\a\X.cs", p);
    }
}
