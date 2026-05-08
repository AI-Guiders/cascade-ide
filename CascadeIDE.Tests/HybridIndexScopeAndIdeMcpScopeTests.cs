using CascadeIDE.Features.HybridIndex.Application;
using CascadeIDE.Features.IdeMcp.Application;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class HybridIndexScopeAndIdeMcpScopeTests
{
    [Fact]
    public void HybridIndexScopeResolver_workspace_mode_nulls_solution()
    {
        var (w, s) = HybridIndexScopeResolver.ApplyScopeMode("workspace", @"C:\repo", @"C:\repo\s.sln");
        Assert.Equal(@"C:\repo", w);
        Assert.Null(s);
    }

    [Fact]
    public void HybridIndexScopeResolver_non_workspace_keeps_solution()
    {
        var (w, s) = HybridIndexScopeResolver.ApplyScopeMode("workspace+solution", @"C:\repo", @"C:\repo\s.sln");
        Assert.Equal(@"C:\repo", w);
        Assert.Equal(@"C:\repo\s.sln", s);
    }

    [Fact]
    public void IdeMcpHybridIndexScope_no_args_uses_solution_and_workspace_resolver()
    {
        var ok = IdeMcpHybridIndexScope.TryResolveForCodebaseIndexCommand(
            argWorkspacePath: null,
            argSolutionPath: null,
            hybridIndexScopeMode: "workspace",
            currentSolutionPath: @"C:\p\a.sln",
            static sln => sln == @"C:\p\a.sln" ? @"C:\p" : null,
            out var ws,
            out var sln,
            out var err);
        Assert.True(ok);
        Assert.Equal(@"C:\p", ws);
        Assert.Null(sln);
        Assert.Null(err);
    }

    [Fact]
    public void IdeMcpHybridIndexScope_no_workspace_returns_error_json()
    {
        var ok = IdeMcpHybridIndexScope.TryResolveForCodebaseIndexCommand(
            null,
            null,
            "workspace",
            @"C:\p\a.sln",
            static _ => "",
            out _,
            out _,
            out var err);
        Assert.False(ok);
        Assert.Contains("no_workspace", err!, StringComparison.Ordinal);
    }

    [Fact]
    public void IdeMcpHybridIndexScope_explicit_workspace_full_paths()
    {
        var ok = IdeMcpHybridIndexScope.TryResolveForCodebaseIndexCommand(
            @"D:\ws",
            null,
            "workspace+solution",
            currentSolutionPath: null,
            static _ => null,
            out var wsRoot,
            out var sln,
            out var err);

        Assert.True(ok);
        Assert.False(string.IsNullOrWhiteSpace(wsRoot));
        Assert.Null(sln);
        Assert.Null(err);
    }
}
