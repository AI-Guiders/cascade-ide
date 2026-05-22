using CascadeIDE.Features.Build.Application;
using CascadeIDE.Features.Launch.Application;
using CascadeIDE.Features.UiChrome.Application;
using CascadeIDE.Features.Workspace.Application;
using CascadeIDE.Models;
using Xunit;

namespace CascadeIDE.Tests;

public sealed class MainWindowStranglerOrchestratorTests
{
    [Fact]
    public void StartupProjectRefresh_empty_when_no_solution()
    {
        var result = StartupProjectRefreshProjection.ResolveAfterSolutionLoad(
            null,
            [],
            @"C:\ws");
        Assert.Equal(StartupProjectRefreshProjection.Empty, result);
    }

    [Fact]
    public void SolutionLoadUiApply_terminal_when_tree_not_in_mfd_slot()
    {
        var plan = SolutionLoadUiApplyProjection.Create(@"C:\a\s.sln", @"C:\a\s.sln", isDockedMfdSolutionExplorerTree: false);
        Assert.Equal(MfdShellPage.Terminal, plan.InitialMfdPage);
    }

    [Fact]
    public void SolutionLoadUiApply_explorer_when_tree_in_mfd_slot()
    {
        var plan = SolutionLoadUiApplyProjection.Create(@"C:\a\s.sln", null, isDockedMfdSolutionExplorerTree: true);
        Assert.Equal(MfdShellPage.SolutionExplorer, plan.InitialMfdPage);
        Assert.Equal(@"C:\a\s.sln", plan.NormalizedSolutionPath);
    }

    [Fact]
    public void BuildSolution_can_build_requires_existing_file()
    {
        Assert.False(MainWindowBuildSolutionPrepProjection.CanBuild(null, isBuilding: false));
        Assert.False(MainWindowBuildSolutionPrepProjection.CanBuild(@"C:\missing.sln", isBuilding: false));
        Assert.False(MainWindowBuildSolutionPrepProjection.CanBuild(@"C:\x.sln", isBuilding: true));
    }

    [Fact]
    public void UiModeLayoutApply_Flight_plan()
    {
        var plan = UiModeLayoutApplyProjection.Create("Flight", persist: false);
        Assert.Equal("Flight", plan.NormalizedMode);
        Assert.True(plan.Spec.TerminalVisible);
        Assert.Equal(2, plan.Spec.EditorGroupCount);
        Assert.Equal(
            MfdShellPage.Build,
            UiModeLayoutApplyProjection.ResolveMfdPageAfterApply(plan, MfdShellPage.Build));

        var terminalTabPlan = new UiModeLayoutApplyProjection.Plan(
            plan.Spec with { SelectTerminalTabWhenTerminalShown = true },
            plan.NormalizedMode,
            plan.Persist);
        Assert.Equal(
            MfdShellPage.Terminal,
            UiModeLayoutApplyProjection.ResolveMfdPageAfterApply(terminalTabPlan, MfdShellPage.Build));
    }
}
